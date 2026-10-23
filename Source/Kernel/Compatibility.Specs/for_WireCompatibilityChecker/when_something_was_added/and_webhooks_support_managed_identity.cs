// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compatibility.for_WireCompatibilityChecker.when_something_was_added;

public class and_webhooks_support_managed_identity : Specification
{
    WireContract _current;
    WireCompatibilityReport _result;

    void Because()
    {
        using var baseline = typeof(and_webhooks_support_managed_identity).Assembly.GetManifestResourceStream("Cratis.Chronicle.Compatibility.Specs.before_managed_identity.desc")!;
        using var buffer = new MemoryStream();
        baseline.CopyTo(buffer);
        _current = WireContractReader.Read(Contracts.WireContractDescriptorSet.Bytes);
        _result = WireCompatibilityChecker.Check(WireContractReader.Read(buffer.ToArray()), _current);
    }

    [Fact] void should_preserve_the_previous_wire_contract() => _result.Incompatibilities.ShouldBeEmpty();
    [Fact] void should_add_the_identity_field() => _current.Messages[".Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookTarget"].Fields[4].Name.ShouldEqual("ManagedIdentity");
    [Fact] void should_append_the_client_id() => _current.Messages[".Cratis.Chronicle.Contracts.Observation.Webhooks.AddWebhookRequest"].Fields[18].Name.ShouldEqual("ManagedIdentityClientId");
    [Fact] void should_append_the_scope() => _current.Messages[".Cratis.Chronicle.Contracts.Observation.Webhooks.AddWebhookRequest"].Fields[19].Name.ShouldEqual("ManagedIdentityScope");
}
