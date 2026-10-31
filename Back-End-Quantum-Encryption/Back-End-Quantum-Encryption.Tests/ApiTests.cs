using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Back_End_Quantum_Encryption.Tests;

public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Simulate_ReturnsStepsAndQubits()
    {
        var response = await _client.PostAsJsonAsync("/api/bb84/simulate", new { qubitCount = 16, seed = 1 });
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(16, json.GetProperty("qubits").GetArrayLength());
        Assert.True(json.GetProperty("steps").GetArrayLength() >= 6);
        // Enums are sent as text.
        Assert.Contains(json.GetProperty("qubits")[0].GetProperty("aliceBasis").GetString(),
            new[] { "Rectilinear", "Diagonal" });
    }

    [Fact]
    public async Task Encrypt_WithoutEve_BobReadsTheMessage()
    {
        var response = await _client.PostAsJsonAsync("/api/bb84/encrypt", new { message = "Hello Bob", seed = 7 });
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Hello Bob", json.GetProperty("bobDecrypted").GetString());
        Assert.False(json.GetProperty("encryption").GetProperty("keyReused").GetBoolean());
    }

    [Fact]
    public async Task Encrypt_WithEve_IsRejected()
    {
        var response = await _client.PostAsJsonAsync("/api/bb84/encrypt",
            new { message = "Hello Bob", eavesdropper = true, seed = 7 });
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("simulation").GetProperty("eavesdropperDetected").GetBoolean());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("encryption").ValueKind);
    }

    [Fact]
    public async Task Decrypt_RoundTripsWithKey()
    {
        var response = await _client.PostAsJsonAsync("/api/bb84/decrypt", new { ciphertextHex = "B1", key = "11110000" });
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("A", json.GetProperty("plaintext").GetString());
    }

    [Fact]
    public async Task InvalidInput_Returns400()
    {
        var tooMany = await _client.PostAsJsonAsync("/api/bb84/simulate", new { qubitCount = 0 });
        var badKey = await _client.PostAsJsonAsync("/api/bb84/decrypt", new { ciphertextHex = "B1", key = "abc" });

        Assert.Equal(HttpStatusCode.BadRequest, tooMany.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badKey.StatusCode);
    }
}
