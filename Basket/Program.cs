using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(builder.Configuration["REDIS"]!));

var app = builder.Build();


app.MapPost("/basket", async (List<BasketItem> item, IConnectionMultiplexer redis) =>
{
    var id = Guid.NewGuid().ToString();
    var db = redis.GetDatabase();
    var serializedItem = JsonSerializer.Serialize(item);
    await db.StringSetAsync(id, serializedItem);
    return Results.Created($"/basket/{id}", item);
});

app.MapGet("/basket/{id}", async (string id, IConnectionMultiplexer redis) =>
{
    var db = redis.GetDatabase();
    var serializedItem = await db.StringGetAsync(id);
    var basketItems = JsonSerializer.Deserialize<List<BasketItem>>(serializedItem);
    return Results.Ok(new Basket(id, basketItems));
});

app.MapPut("/basket/{id}", async (string id, List<BasketItem> item, IConnectionMultiplexer redis) =>
{
    var db = redis.GetDatabase();
    var serializedItem = JsonSerializer.Serialize(item);
    await db.StringSetAsync(id, serializedItem);
    return Results.Ok(new Basket(id, item));
});

app.MapDelete("/basket/{id}", async (string id, IConnectionMultiplexer redis) =>
{
    var db = redis.GetDatabase();
    await db.KeyDeleteAsync(id);
    return Results.NoContent();
});

app.Run();

record Basket(string Id, List<BasketItem> Items);
record BasketItem(string Name, int Quantity, decimal Price);