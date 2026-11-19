// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using ContractMode = Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookSigningMode;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookDefinitionConverters;

public class when_mapping_signing : Specification
{
    Contracts.Observation.Webhooks.WebhookDefinition _definition;

    void Establish() => _definition = new()
    {
        Identifier = "webhook",
        EventSequenceId = "log",
        Target = new() { Url = "https://example.com/webhook" }
    };

    [Theory]
    [InlineData(ContractMode.Unspecified, WebhookSigningMode.None)]
    [InlineData(ContractMode.Disabled, WebhookSigningMode.None)]
    [InlineData(ContractMode.Ed25519, WebhookSigningMode.Ed25519)]
    void should_map_registration_values(ContractMode contract, WebhookSigningMode expected)
    {
        _definition.Signing = contract;
        _definition.ToChronicle().Signing.ShouldEqual(expected);
    }

    [Theory]
    [InlineData(WebhookSigningMode.None, ContractMode.Disabled)]
    [InlineData(WebhookSigningMode.Ed25519, ContractMode.Ed25519)]
    void should_report_explicit_effective_values(WebhookSigningMode signing, ContractMode expected)
    {
        var definition = _definition.ToChronicle() with { Signing = signing };
        definition.ToContract().Signing.ShouldEqual(expected);
        definition.ToReadModel().Signing.ShouldEqual(expected);
    }

    [Fact]
    void should_default_legacy_added_events_to_unsigned() =>
        new WebhookAdded(WebhookOwner.Client, "log", [], "https://example.com/webhook", new Dictionary<string, string>(), true, true).Signing.ShouldEqual(WebhookSigningMode.None);
}
