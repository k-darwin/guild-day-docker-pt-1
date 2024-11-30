using System.Net.WebSockets;
using System.Text;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.WebSockets;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Configure the WebSocket endpoint
app.UseWebSockets();

// Collection to keep track of connected WebSocket clients
ConcurrentDictionary<WebSocket, bool> clients = new ConcurrentDictionary<WebSocket, bool>();

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
            clients.TryAdd(webSocket, true);

            await Echo(webSocket);
        }
        else
        {
            context.Response.StatusCode = 400;
        }
    }
    else
    {
        await next();
    }
});

async Task Echo(WebSocket webSocket)
{
    var buffer = new byte[1024 * 4];
    WebSocketReceiveResult result = null;

    try
    {
        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        while (!result.CloseStatus.HasValue)
        {
            await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);
            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"WebSocket error: {ex.Message}");
    }
    finally
    {
        clients.TryRemove(webSocket, out _);
        if (result != null)
        {
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }
    }
}

var mongoClient = new MongoClient(builder.Configuration["MongoConnectionString"]);
var database = mongoClient.GetDatabase("reports");
var reportCollection = database.GetCollection<OrderReport>("order-reports");

var client = new ServiceBusClient(builder.Configuration["ServiceBusConnectionString"]);
var processor = client.CreateProcessor("order.created", new ServiceBusProcessorOptions());

// Handlers for message processing
processor.ProcessMessageAsync += async args =>
{
    var body = args.Message.Body.ToString();
    var orderUpdate = JsonSerializer.Deserialize<Order>(body);
    
    var report = new OrderReport
    {
        OrderId = orderUpdate!.Id.ToString(),
        Items = orderUpdate.OrderItems.Count,
        Cost = orderUpdate.Cost,
        UpdatedAt = DateTime.UtcNow
    };

    await reportCollection.InsertOneAsync(report);

    var orderReports = new List<OrderReport>();
    using (var cursor = await reportCollection.FindAsync(r => true))
    {
        while (await cursor.MoveNextAsync())
        {
            orderReports.AddRange(cursor.Current);
        }
    }
    var orderReportSummary = new OrderReportSummary
    {
        TotalItems = orderReports.Sum(r => r.Items),
        TotalOrders = orderReports.Count,
        TotalCost = orderReports.Sum(r => r.Cost)
    };

    var summaryJson = JsonSerializer.Serialize(orderReportSummary);

    foreach (var webSocket in clients.Keys.Where(s => s.State == WebSocketState.Open))
    {
        var message = Encoding.UTF8.GetBytes(summaryJson);
        await webSocket.SendAsync(message, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    await args.CompleteMessageAsync(args.Message);
};


processor.ProcessErrorAsync += args =>
{
    Console.WriteLine(args.Exception.ToString());
    return Task.CompletedTask;
};

// Start processing
await processor.StartProcessingAsync();

// Run the app
app.Run();

public class OrderReportSummary
{
    public int TotalItems { get; set; }
    public int TotalOrders { get; set;}
    public decimal TotalCost { get; set; }
}

public class OrderReport
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    public string? OrderId { get; set; }
    public int Items { get; set; }
    public decimal Cost { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Order
{
    public int Id { get; set; }
    public decimal Cost { get; set; }
    public List<OrderItem> OrderItems { get; set; }
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Cost { get; set; }
}