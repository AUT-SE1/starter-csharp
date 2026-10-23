using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace StarterService;

/// <summary>The response body of POST /orders and GET /orders/{id}.</summary>
public sealed record Order(string Id, string Item, int Quantity, string Status);

/// <summary>
/// The request body of POST /orders. Both fields are nullable so a missing
/// field can be told apart from a zero value: {"quantity": 0} and {} must
/// produce different messages.
/// </summary>
public sealed record CreateOrderRequest(string? Item, int? Quantity);

/// <summary>
/// The body returned with every 4xx response. Null fields are left out rather
/// than written as null: the shared contract types <c>field</c> and
/// <c>message</c> as strings, and a null would fail a neighbouring team's
/// schema validation.
/// </summary>
public sealed record ApiError(
    string Error,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Field,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Message)
{
    public static ApiError Validation(string field, string message) =>
        new(Orders.ErrValidation, field, message);

    public static ApiError NotFound(string message) =>
        new(Orders.ErrNotFound, null, message);
}

/// <summary>Thrown when a request body does not satisfy the contract.</summary>
public sealed class ValidationException(string field, string message) : Exception(message)
{
    public string Field { get; } = field;

    public ApiError AsBody() => ApiError.Validation(Field, Message);
}

/// <summary>
/// Order validation, kept apart from the web layer so the tests for it are
/// unit tests: no HTTP, no network, no storage.
///
/// The bounds are part of the shared contract in openapi.yaml. Keep them and
/// the specification in step with each other.
/// </summary>
public static class Orders
{
    public const int MinQuantity = 1;
    public const int MaxQuantity = 1000;
    public const int MaxItemLength = 64;

    public const string StatusCreated = "created";
    public const string ErrValidation = "validation_failed";
    public const string ErrNotFound = "not_found";

    /// <summary>
    /// Checks one request body and returns the cleaned values.
    /// </summary>
    /// <exception cref="ValidationException">names the first field that is wrong.</exception>
    public static (string Item, int Quantity) ValidateCreateOrder(string? item, int? quantity)
    {
        if (item is null)
        {
            throw new ValidationException("item", "item is required");
        }

        var cleaned = item.Trim();
        if (cleaned.Length == 0)
        {
            throw new ValidationException("item", "item must not be empty");
        }

        // The limit is 64 characters, not 64 UTF-16 code units, so a surrogate
        // pair counts once.
        var characters = new System.Globalization.StringInfo(cleaned).LengthInTextElements;
        if (characters > MaxItemLength)
        {
            throw new ValidationException("item", "item is longer than 64 characters");
        }

        if (quantity is null)
        {
            throw new ValidationException("quantity", "quantity is required");
        }

        if (quantity < MinQuantity || quantity > MaxQuantity)
        {
            throw new ValidationException("quantity", "quantity must be between 1 and 1000");
        }

        return (cleaned, quantity.Value);
    }
}

/// <summary>Orders held in memory. A real service would use a database.</summary>
public sealed class OrderStore
{
    private readonly ConcurrentDictionary<string, Order> _orders = new();

    public Order Create(string item, int quantity)
    {
        var order = new Order(NewId(), item, quantity, Orders.StatusCreated);
        _orders[order.Id] = order;
        return order;
    }

    public Order? Get(string id) => _orders.TryGetValue(id, out var order) ? order : null;

    /// <summary>Reset between tests, so no test inherits another test's state.</summary>
    public void Clear() => _orders.Clear();

    private static string NewId() =>
        "ord_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant();
}
