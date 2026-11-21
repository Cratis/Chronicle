```csharp
using Cratis.Chronicle.Webhooks;

app.MapPost("/chronicle/webhooks", async (HttpRequest request, IWebhookSignatureVerifier verifier) =>
{
    using var buffer = new MemoryStream();
    await request.Body.CopyToAsync(buffer);

    var result = verifier.Verify(
        request.Headers[WebhookSigningHeaders.Id],
        request.Headers[WebhookSigningHeaders.Timestamp],
        request.Headers[WebhookSigningHeaders.Signature],
        buffer.ToArray());

    if (!result.IsValid)
    {
        return Results.Unauthorized();
    }

    // The body is authentic: deserialize buffer and process the events.
    return Results.Ok();
});
```
