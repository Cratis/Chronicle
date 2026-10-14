// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Integration.for_Webhooks.given;

public class all_dependencies(ChronicleFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
{
    readonly Uri _target = new($"http://webhook-{Guid.NewGuid():N}.test/webhooks");
    WebhookReceiver? _receiver;
    IDisposable? _receiverLease;

    public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
    public InvokedWebhooks InvokedWebhooks = new();
    public IWebhooks Webhooks => Services.GetService<IEventStore>().Webhooks;

    public WebhookTargetUrl TargetUrl => _target.AbsoluteUri;

    protected override void ConfigureServices(IServiceCollection services)
    {
        _receiver ??= new(InvokedWebhooks);
        _receiverLease ??= TestHttpClientFactory.Instance.Register(_target, _receiver);
        services.AddSingleton(InvokedWebhooks);

        // WebhookMediator retains the first factory it resolves. Every context supplies the same
        // dispatcher, but leases a distinct address so no request can reach another context's collector.
        services.AddSingleton<IHttpClientFactory>(TestHttpClientFactory.Instance);
    }

    protected override async Task OnDisposeAsync()
    {
        DisposeReceiver();
        await base.OnDisposeAsync();
    }

    public override void Dispose()
    {
        DisposeReceiver();
        base.Dispose();
    }

    void DisposeReceiver()
    {
        _receiverLease?.Dispose();
        _receiver?.Dispose();
    }

    protected override void ConfigureWebHostBuilder(IWebHostBuilder builder)
    {
        builder.Configure(app => app
            .UseRouting()
            .UseEndpoints(b =>
            {
                b.MapPost("/webhooks", async httpContext =>
                {
                    var invokedWebhooks = httpContext.RequestServices.GetRequiredService<InvokedWebhooks>();
                    using var reader = new StreamReader(httpContext.Request.Body);
                    var body = await reader.ReadToEndAsync();

                    invokedWebhooks.Add(body, httpContext.Request.Headers
                        .Where(pair => !new[] { "Host", "Content-Type", "Content-Length" }.Contains(pair.Key))
                        .ToDictionary(k => k.Key, v => v.Value.ToString()));
                    httpContext.Response.StatusCode = 200;
                    await httpContext.Response.WriteAsync("OK");
                });
            }));
    }

    sealed class WebhookReceiver(InvokedWebhooks invokedWebhooks) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            var headers = request.Headers.Concat(request.Content.Headers)
                .Where(pair => !new[] { "Host", "Content-Type", "Content-Length" }.Contains(pair.Key, StringComparer.OrdinalIgnoreCase))
                .ToDictionary(pair => pair.Key, pair => string.Join(',', pair.Value));
            cancellationToken.ThrowIfCancellationRequested();
            invokedWebhooks.Add(body, headers);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("OK") };
        }
    }
}
