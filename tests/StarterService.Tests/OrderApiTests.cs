using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace StarterService.Tests;

/// <summary>
/// Integration tests: routing, JSON encoding and the store, working together.
/// WebApplicationFactory boots the real application in memory.
/// </summary>
public class OrderApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OrderApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private HttpClient NewClient()
    {
        var client = _factory.CreateClient();
        // Each test starts from an empty store, so no test inherits another's
        // state and the order they run in does not matter.
        _factory.Services.GetRequiredService<OrderStore>().Clear();
        return client;
    }

    [Fact]
    public async Task PostOrders_WithAValidBody_Returns201AndTheOrder()
    {
        var client = NewClient();

        var response = await client.PostAsJsonAsync("/orders", new { item = "notebook", quantity = 3 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("notebook", body.GetProperty("item").GetString());
        Assert.Equal(3, body.GetProperty("quantity").GetInt32());
        Assert.Equal("created", body.GetProperty("status").GetString());
        Assert.StartsWith("ord_", body.GetProperty("id").GetString());
    }

    [Fact]
    public async Task PostOrders_WithAnInvalidQuantity_Returns400()
    {
        var client = NewClient();

        var response = await client.PostAsJsonAsync("/orders", new { item = "notebook", quantity = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("validation_failed", body.GetProperty("error").GetString());
        Assert.Equal("quantity", body.GetProperty("field").GetString());
    }

    [Fact]
    public async Task PostOrders_WithMalformedJson_Returns400()
    {
        var client = NewClient();

        var response = await client.PostAsync("/orders",
            new StringContent("{\"item\":", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetOrder_AfterCreating_ReturnsTheSameOrder()
    {
        var client = NewClient();
        var created = await (await client.PostAsJsonAsync("/orders", new { item = "کتاب", quantity = 7 }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();

        var response = await client.GetAsync($"/orders/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(id, body.GetProperty("id").GetString());
        Assert.Equal("کتاب", body.GetProperty("item").GetString());
        Assert.Equal(7, body.GetProperty("quantity").GetInt32());
    }

    [Fact]
    public async Task GetOrder_WhenIdIsUnknown_Returns404()
    {
        var client = NewClient();

        var response = await client.GetAsync("/orders/ord_missing");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("not_found", body.GetProperty("error").GetString());
        Assert.False(body.TryGetProperty("field", out _), "the contract leaves field out when it is null");
    }

    [Fact]
    public async Task Health_Returns200AndStatusOk()
    {
        var client = NewClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ok", body.GetProperty("status").GetString());
    }
}
