// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Webhooks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Cratis.Chronicle.InProcess.Integration.for_Webhooks.given;

public class all_dependencies(ChronicleInProcessFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
{
    public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
    public InvokedWebhooks InvokedWebhooks;
    public IWebhooks Webhooks => Services.GetService<IEventStore>().Webhooks;

    public WebhookTargetUrl TargetUrl
    {
        get
        {
            var server = Services.GetRequiredService<IServer>();
            var addressFeature = server.Features.Get<IServerAddressesFeature>();
            var baseAddress = addressFeature?.Addresses.FirstOrDefault() ?? "http://localhost:5000";
            return $"{baseAddress.TrimEnd('/')}/webhooks";
        }
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        InvokedWebhooks = new();

        // Override the IHttpClientFactory to use TestServer's handler
        services.AddTransient<IHttpClientFactory>(_ => new TestHttpClientFactory(CreateClient()));
    }

    protected override void ConfigureWebHostBuilder(IWebHostBuilder builder)
    {
        builder.Configure(app => app
            .UseRouting()
            .UseEndpoints(b =>
            {
                b.MapPost("/webhooks", async httpContext =>
                {
                    using var reader = new StreamReader(httpContext.Request.Body);
                    var body = await reader.ReadToEndAsync();

                    InvokedWebhooks.Add(body, httpContext.Request.Headers
                        .Where(pair => !new[] { "Host", "Content-Type", "Content-Length" }.Contains(pair.Key))
                        .ToDictionary(k => k.Key, v => v.Value.ToString()));
                    httpContext.Response.StatusCode = 200;
                    await httpContext.Response.WriteAsync("OK");
                });
            }));
    }
}
