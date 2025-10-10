// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.InProcess.Integration.for_Webhooks.given;

public class all_dependencies(ChronicleInProcessFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
{
    public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
    public InvokedWebhooks InvokedWebhooks;
    public IWebhooks Webhooks => Services.GetService<IEventStore>().Webhooks;
    
    public WebhookTargetUrl TargetUrl => "http://localhost/webhooks";

    protected override void ConfigureServices(IServiceCollection services)
    {
         InvokedWebhooks = new();

         services.AddSingleton(InvokedWebhooks);
    }
}