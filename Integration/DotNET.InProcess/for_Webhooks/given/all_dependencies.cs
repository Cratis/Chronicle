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


    public WebhookTargetUrl TargetUrl => $"{Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault() ?? "http://localhost"}/webhooks";

    protected override void ConfigureServices(IServiceCollection services)
    {
         InvokedWebhooks = new();
         services.AddSingleton(InvokedWebhooks);

         // Configure the webhook HTTP client to use the test server
         // services.AddHttpClient("webhook")
         //     .ConfigurePrimaryHttpMessageHandler(() => Services.GetRequiredService<IServer>().);
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
                    Console.WriteLine($"Received webhook: {body}");

                    // Store the invoked webhook for test verification
                    var invokedWebhooks = httpContext.RequestServices.GetService<InvokedWebhooks>();
                    invokedWebhooks?.Add(body);
                });
            }));
    }
}