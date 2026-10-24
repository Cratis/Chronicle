// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Validation;
using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_binding_configuration;

public class and_webhook_validation_is_configured : Specification
{
    ChronicleOptions _options;

    void Establish()
    {
        _options = new ChronicleOptions();
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cratis:Chronicle:Webhooks:Validation"] = "Enforce",
            ["Cratis:Chronicle:Webhooks:AllowInsecureHttp"] = "true",
            ["Cratis:Chronicle:Webhooks:AllowedHosts:0"] = "*.example.com",
            ["Cratis:Chronicle:Webhooks:EventStoreAllowedHosts:orders:0"] = "orders.example.com"
        }).Build().GetSection(ChronicleOptions.SectionPath).Bind(_options);
    }

    [Fact] void should_bind_enforce() => _options.Webhooks.Validation.ShouldEqual(WebhookValidationMode.Enforce);
    [Fact] void should_bind_insecure_http() => _options.Webhooks.AllowInsecureHttp.ShouldBeTrue();
    [Fact] void should_bind_global_hosts() => _options.Webhooks.AllowedHosts.ShouldContain("*.example.com");
    [Fact] void should_bind_event_store_hosts() => _options.Webhooks.EventStoreAllowedHosts["orders"].ShouldContain("orders.example.com");
}
