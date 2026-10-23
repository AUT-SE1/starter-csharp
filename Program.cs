using System.Text.Json;
using StarterService;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<OrderStore>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/orders", async (HttpRequest request, OrderStore store) =>
{
    CreateOrderRequest? body;
    try
    {
        body = await JsonSerializer.DeserializeAsync<CreateOrderRequest>(
            request.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
    catch (JsonException)
    {
        return Results.Json(
            ApiError.Validation("body", "request body is not valid JSON"),
            statusCode: StatusCodes.Status400BadRequest);
    }

    try
    {
        var (item, quantity) = Orders.ValidateCreateOrder(body?.Item, body?.Quantity);
        return Results.Json(store.Create(item, quantity),
            statusCode: StatusCodes.Status201Created);
    }
    catch (ValidationException failure)
    {
        return Results.Json(failure.AsBody(), statusCode: StatusCodes.Status400BadRequest);
    }
});

app.MapGet("/orders/{id}", (string id, OrderStore store) =>
{
    var order = store.Get(id);
    return order is null
        ? Results.Json(ApiError.NotFound("no order with that id"),
            statusCode: StatusCodes.Status404NotFound)
        : Results.Json(order, statusCode: StatusCodes.Status200OK);
});

app.Run();

/// <summary>
/// Exposed so the integration tests can boot the real application with
/// WebApplicationFactory. Top-level statements compile into an internal
/// Program class; this makes it visible to the test project.
/// </summary>
public partial class Program;
