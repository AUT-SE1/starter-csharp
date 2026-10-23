using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace StarterService.Tests.Contract;

/// <summary>
/// Session 05 — contract test against the NEIGHBOURING TEAM's running sandbox.
///
/// This is not a mock. It sends real HTTP to a service you did not write and
/// checks the answer against the OpenAPI document both teams agreed on.
///
/// Run it:
///   NEIGHBOR_BASE_URL=http://10.0.0.42:8080 make contract-test
///
/// With no NEIGHBOR_BASE_URL set, every test here is skipped, so `make test`
/// stays green on a laptop with no neighbour in reach. A skipped contract test
/// is not a passing one — your report must show it running against a real
/// address.
///
/// TODO(team): two things to change, and nothing else.
///   1. NeighborOrdersPath, if your neighbour's service names its routes
///      differently from the shared contract.
///   2. ValidOrderBody(), if their CreateOrderRequest carries extra fields.
/// </summary>
public class NeighborContractTests
{
    /// <summary>TODO(team): confirm against their OpenAPI.</summary>
    private const string NeighborOrdersPath = "/orders";

    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

    private readonly ITestOutputHelper _output;

    public NeighborContractTests(ITestOutputHelper output) => _output = output;

    /// <summary>TODO(team): match your neighbour's CreateOrderRequest schema.</summary>
    private static object ValidOrderBody() => new { item = "contract-test-item", quantity = 2 };

    private static string? BaseUrl() =>
        Environment.GetEnvironmentVariable("NEIGHBOR_BASE_URL")?.TrimEnd('/');

    private static OpenApiCheck Document() =>
        OpenApiCheck.Load(Path.Combine(AppContext.BaseDirectory, "openapi.json"));

    private static HttpClient NewClient() =>
        new(new HttpClientHandler { UseProxy = false }) { Timeout = RequestTimeout };

    private async Task<(int Status, JsonElement Body)> SendAsync(
        HttpMethod method, string url, object? jsonBody)
    {
        using var client = NewClient();
        using var request = new HttpRequestMessage(method, url);
        if (jsonBody is not null)
        {
            request.Content = JsonContent.Create(jsonBody);
        }

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request);
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException)
        {
            throw new Xunit.Sdk.XunitException(
                $"{method} {url} failed to reach the neighbour's sandbox: {error.Message}\n"
                + "Is their service running, listening on 0.0.0.0 and allowed through the firewall?");
        }

        // A timestamp belongs in the report, so print one with every call.
        _output.WriteLine($"{DateTimeOffset.Now:O}  {method} {url} -> {(int)response.StatusCode}");

        var text = await response.Content.ReadAsStringAsync();
        try
        {
            using var document = JsonDocument.Parse(text);
            return ((int)response.StatusCode, document.RootElement.Clone());
        }
        catch (JsonException)
        {
            throw new Xunit.Sdk.XunitException(
                $"{method} {url} did not return JSON. Body was: {text[..Math.Min(400, text.Length)]}");
        }
    }

    private static void AssertMatches(OpenApiCheck document, string schemaName, JsonElement value)
    {
        var problems = document.Validate(schemaName, value);
        Assert.True(problems.Count == 0,
            $"response does not match the agreed {schemaName} schema:\n  "
            + string.Join("\n  ", problems));
    }

    // --- the contract -------------------------------------------------------

    [SkippableFact]
    public async Task NeighborCreateOrder_Returns201AndAnOrderMatchingTheContract()
    {
        var baseUrl = RequireNeighbor();
        var document = Document();

        var (status, body) = await SendAsync(HttpMethod.Post, baseUrl + NeighborOrdersPath, ValidOrderBody());

        Assert.True(status == 201, $"expected 201, got {status} — body: {body}");
        AssertMatches(document, "Order", body);
    }

    [SkippableFact]
    public async Task NeighborCreateOrder_WithQuantityZero_Returns400AndAnError()
    {
        var baseUrl = RequireNeighbor();
        var document = Document();

        var (status, body) = await SendAsync(HttpMethod.Post, baseUrl + NeighborOrdersPath,
            new { item = "contract-test-item", quantity = 0 });

        // The contract fixes the status code and the error shape. It says
        // nothing about the wording of the message — that is their business.
        Assert.True(status == 400, $"expected 400 for an invalid quantity, got {status} — body: {body}");
        AssertMatches(document, "Error", body);
    }

    [SkippableFact]
    public async Task NeighborGetOrder_AfterCreating_ReturnsTheSameOrder()
    {
        var baseUrl = RequireNeighbor();
        var document = Document();

        var (_, created) = await SendAsync(HttpMethod.Post, baseUrl + NeighborOrdersPath, ValidOrderBody());
        var id = created.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
        Assert.False(string.IsNullOrEmpty(id), $"the create response carried no usable id: {created}");

        var (status, body) = await SendAsync(HttpMethod.Get, $"{baseUrl}{NeighborOrdersPath}/{id}", null);

        Assert.True(status == 200, $"expected 200 when reading back order {id}, got {status}");
        AssertMatches(document, "Order", body);
        Assert.Equal(id, body.GetProperty("id").GetString());
    }

    [SkippableFact]
    public async Task NeighborGetOrder_WithAnUnknownId_Returns404()
    {
        var baseUrl = RequireNeighbor();
        var document = Document();

        var (status, body) = await SendAsync(
            HttpMethod.Get, $"{baseUrl}{NeighborOrdersPath}/ord_definitely_not_real", null);

        Assert.True(status == 404, $"expected 404 for an unknown id, got {status} — body: {body}");
        AssertMatches(document, "Error", body);
    }

    private static string RequireNeighbor()
    {
        var baseUrl = BaseUrl();
        Skip.If(string.IsNullOrWhiteSpace(baseUrl),
            "NEIGHBOR_BASE_URL is not set — skipping the contract test. "
            + "Set it to your neighbour's sandbox, e.g. http://10.0.0.42:8080");
        return baseUrl!;
    }
}
