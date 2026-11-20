using System.Net;
using System.Text;
using System.Text.Json;
using BuildWise.Data;
using BuildWise.Web.Services.Scopes;
using BuildWise.Web.Services.Quotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Gemini:ApiKey"] = "test-key",
    ["Gemini:Models:0"] = "first-model",
    ["Gemini:Models:1"] = "fallback-model",
    ["Gemini:Models:2"] = "first-model",
    ["Gemini:ScopeModelTimeoutSeconds"] = "1",
    ["Gemini:ScopeTimeoutSeconds"] = "15"
}).Build();
var serviceId = Guid.NewGuid();
var request = new ClientRequest { Title = "House tubing", Description = "Install conduit; measurements need assessment." };
var services = new[] { new Service { Id = serviceId, Name = "Conduit installation", IsActive = true } };
GeminiScopeDraftService Service(HttpMessageHandler handler) => new(
    new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan }, configuration,
    NullLogger<GeminiScopeDraftService>.Instance);
var generated = JsonSerializer.Serialize(new
{
    items = new[] { new { serviceId, title = "Conduit installation", description = "Confirm routing and measurements on site.", quantity = (int?)null, unitCode = (string?)null, requireAssessment = true, clauses = Array.Empty<object>() } }
});
HttpResponseMessage Success() => new(HttpStatusCode.OK)
{
    Content = new StringContent(JsonSerializer.Serialize(new { output_text = generated }), Encoding.UTF8, "application/json")
};
async Task<string> ReadModel(HttpRequestMessage message, CancellationToken token)
{
    using var body = JsonDocument.Parse(await message.Content!.ReadAsStringAsync(token));
    var settings = body.RootElement.GetProperty("generation_config");
    if (settings.GetProperty("thinking_level").GetString() != "low" || settings.GetProperty("max_output_tokens").GetInt32() != 8192)
        throw new Exception("Scope generation settings were not preserved.");
    return body.RootElement.GetProperty("model").GetString()!;
}
foreach (var failure in new[] { 400, 404, 410, 422, 429, 500, 503 })
{
    var models = new List<string>();
    var service = Service(new Handler(async (message, token) =>
    {
        models.Add(await ReadModel(message, token));
        return models.Count == 1 ? new HttpResponseMessage((HttpStatusCode)failure) : Success();
    }));
    var items = await service.GenerateAsync(request, services);
    if (!models.SequenceEqual(new[] { "first-model", "fallback-model" }) || items.Single().ServiceId != serviceId)
        throw new Exception($"HTTP {failure} must advance to the next model without repeating the first model.");
}
foreach (var denied in new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden })
{
    var calls = 0;
    var service = Service(new Handler((_, _) =>
    {
        calls++;
        return Task.FromResult(new HttpResponseMessage(denied));
    }));
    try { await service.GenerateAsync(request, services); throw new Exception("Key failures must stop generation."); }
    catch (ScopeDraftUnavailableException exception) when (!exception.TryNextModel && calls == 1) { }
}
var timedOutModels = new List<string>();
var recovering = Service(new Handler(async (message, token) =>
{
    timedOutModels.Add(await ReadModel(message, token));
    return timedOutModels.Count == 1
        ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new StalledStream()) }
        : Success();
}));
if ((await recovering.GenerateAsync(request, services)).Count != 1 || timedOutModels.Count != 2)
    throw new Exception("A response-body timeout must advance to the next model.");
var unavailableCalls = 0;
var unavailable = Service(new Handler((_, _) =>
{
    unavailableCalls++;
    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
}));
try { await unavailable.GenerateAsync(request, services); throw new Exception("Exhausted models must fail clearly."); }
catch (ScopeDraftUnavailableException exception) when (exception.Message.StartsWith("None of") && unavailableCalls == 2) { }
using var cancelled = new CancellationTokenSource();
cancelled.Cancel();
try { await unavailable.GenerateAsync(request, services, cancelled.Token); throw new Exception("Caller cancellation must propagate."); }
catch (OperationCanceledException) { }
if (unavailableCalls != 2) throw new Exception("Cancellation must not call another model.");
Console.WriteLine("PASS: ordered unique models, unsupported/retired models, quota and server failures, body timeout fallback, key failures, exhaustion and caller cancellation.");

var wordingModels = new List<string>();
var wordingPayload = JsonSerializer.Serialize(new
{
    items = new[] { new { scopeItemId = serviceId, description = "Install conduit along the agreed route." } },
    assumptionsAndExclusions = "Site measurements to be confirmed."
});
var wordingHandler = new Handler(async (message, token) =>
{
    wordingModels.Add(await ReadModel(message, token));
    return wordingModels.Count == 1 ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        : new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new { output_text = wordingPayload }), Encoding.UTF8, "application/json")
        };
});
var wording = new GeminiQuotationWordingService(new HttpClient(wordingHandler), configuration,
    NullLogger<GeminiQuotationWordingService>.Instance);
var source = new[] { new QuotationScopeSource(serviceId, "Conduit", "Install conduit", null, null, [], []) };
var prepared = await wording.GenerateAsync(source);
if (!wordingModels.SequenceEqual(new[] { "first-model", "fallback-model" })
    || !prepared.ItemDescriptions.ContainsKey(serviceId))
    throw new Exception("Quotation wording must advance to the next model and retain scope item IDs.");
Console.WriteLine("PASS: quotation wording uses the configured next-model fallback and validates generated item IDs.");

sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
}
sealed class StalledStream : Stream
{
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() => throw new NotSupportedException();
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await Task.Delay(Timeout.Infinite, cancellationToken);
        return 0;
    }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
