// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Serverless;
using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.for_ChronicleOptions.when_binding_from_configuration;

public class with_serverless_settings : Specification
{
    ChronicleOptions _result;

    void Because()
    {
        var configuration = new Dictionary<string, string?>
        {
            ["Chronicle:Serverless:Mode"] = "Serverless",
            ["Chronicle:Serverless:ReceiverUrl"] = "https://orders.example.com",
            ["Chronicle:Serverless:ReceiverPath"] = "/deliveries",
            ["Chronicle:Serverless:Registration"] = "OnColdStart",
            ["Chronicle:Serverless:Auth:Type"] = "None",
            ["Chronicle:Serverless:Auth:AllowUnauthenticated"] = "true",
            ["Chronicle:Serverless:DefaultDelivery"] = "Stream",
            ["Chronicle:Serverless:DeploymentGeneration"] = "7",
            ["Chronicle:Serverless:AllowOnColdStartOutsideDevelopment"] = "true",
            ["Chronicle:Serverless:Observers:Some.Reactor:Delivery"] = "Stream"
        };

        var configurationRoot = new ConfigurationBuilder()
            .AddInMemoryCollection(configuration)
            .Build();

        _result = new ChronicleOptions();
        configurationRoot.GetSection("Chronicle").Bind(_result);
    }

    [Fact] void should_bind_mode() => _result.Serverless.Mode.ShouldEqual(ServerlessMode.Serverless);
    [Fact] void should_bind_receiver_url() => _result.Serverless.ReceiverUrl.ShouldEqual(new Uri("https://orders.example.com"));
    [Fact] void should_bind_receiver_path() => _result.Serverless.ReceiverPath.ShouldEqual("/deliveries");
    [Fact] void should_bind_registration() => _result.Serverless.Registration.ShouldEqual(ServerlessRegistration.OnColdStart);
    [Fact] void should_bind_auth_type() => _result.Serverless.Auth.Type.ShouldEqual(ReceiverAuthenticationType.None);
    [Fact] void should_bind_allow_unauthenticated() => _result.Serverless.Auth.AllowUnauthenticated.ShouldBeTrue();
    [Fact] void should_bind_default_delivery() => _result.Serverless.DefaultDelivery.ShouldEqual(ObserverDelivery.Stream);
    [Fact] void should_bind_deployment_generation() => _result.Serverless.DeploymentGeneration.ShouldEqual(7UL);
    [Fact] void should_bind_the_cold_start_acknowledgement() => _result.Serverless.AllowOnColdStartOutsideDevelopment.ShouldBeTrue();
    [Fact] void should_bind_the_observer_override() => _result.Serverless.Observers["Some.Reactor"].Delivery.ShouldEqual(ObserverDelivery.Stream);
}
