// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Webhooks.for_WebhookDefinitionConverters.when_round_tripping;

public class with_ed25519_signing : Specification
{
    Concepts.Observation.Webhooks.WebhookDefinition _definition;
    WebhookDefinition _stored;
    Concepts.Observation.Webhooks.WebhookDefinition _result;

    void Establish() => _definition = new("webhook", WebhookOwner.Client, "log", [], new("https://example.com", Concepts.Observation.Webhooks.WebhookAuthorization.None, new Dictionary<string, string>())) { Signing = WebhookSigningMode.Ed25519 };
    void Because()
    {
        _stored = _definition.ToMongoDB();
        _result = _stored.ToKernel();
    }

    [Fact] void should_persist_the_signing_policy() => _stored.Signing.ShouldEqual(WebhookSigningMode.Ed25519);
    [Fact] void should_restore_the_signing_policy() => _result.Signing.ShouldEqual(WebhookSigningMode.Ed25519);
    [Fact] void should_leave_new_definitions_unsigned_by_default() => new Concepts.Observation.Webhooks.WebhookDefinition("webhook", WebhookOwner.Client, "log", [], new("https://example.com", Concepts.Observation.Webhooks.WebhookAuthorization.None, new Dictionary<string, string>())).Signing.ShouldEqual(WebhookSigningMode.None);
    [Fact] void should_leave_legacy_documents_unsigned() => new WebhookDefinition().ToKernel().Signing.ShouldEqual(WebhookSigningMode.None);
}
