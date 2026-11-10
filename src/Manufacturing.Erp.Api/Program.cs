using System.Text.Json.Serialization;
using Manufacturing.Erp.Domain;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ErpStore>();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
var app = builder.Build();
app.Use(async (context, next) =>
{
    try { await next(context); }
    catch (BusinessException exception)
    {
        await Results.Problem(statusCode: exception.Status, title: exception.Message).ExecuteAsync(context);
    }
});
app.MapGet("/health", () => new { status = "ok", storage = "in-memory", simulation = true });
app.MapGet("/api/catalog", (ErpStore store) => new { store.Materials, store.Suppliers, store.Warehouses });
app.MapGet("/api/purchase-orders", (ErpStore store) => store.ListOrders());
app.MapGet("/api/purchase-orders/{id:guid}", (Guid id, ErpStore store) => store.GetOrder(id));
app.MapPost("/api/purchase-orders", (NewOrder input, ErpStore store) =>
{
    var order = store.Create(input);
    return Results.Created($"/api/purchase-orders/{order.Id}", order);
});
app.MapPost("/api/purchase-orders/{id:guid}/submit", (Guid id, ActorRequest input, ErpStore store) => store.Submit(id, input.Actor));
app.MapPost("/api/purchase-orders/{id:guid}/approve", (Guid id, ActorRequest input, ErpStore store) => store.Approve(id, input.Actor));
app.MapPost("/api/purchase-orders/{id:guid}/receipts", (Guid id, ReceiveRequest input, ErpStore store) => store.Receive(id, input));
app.MapGet("/api/inventory/balances", (ErpStore store) => store.GetBalances());
app.MapGet("/api/inventory/ledger", (ErpStore store) => store.GetLedger());
app.Run();

public sealed record ActorRequest(string Actor);
