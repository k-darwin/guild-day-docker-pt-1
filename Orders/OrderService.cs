using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;

public class OrderService
{
    private readonly AppDbContext _context;
    private readonly ServiceBusClient _serviceBusClient;

    public OrderService(AppDbContext context, ServiceBusClient serviceBusClient)
    {
        _context = context;
        _serviceBusClient = serviceBusClient;
    }

    public async Task SaveOrderAsync(Order order)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Send message to Azure Queue
        ServiceBusSender sender = _serviceBusClient.CreateSender("order.created");
        ServiceBusMessage message = new ServiceBusMessage(JsonSerializer.Serialize(order));
        await sender.SendMessageAsync(message);
    }
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