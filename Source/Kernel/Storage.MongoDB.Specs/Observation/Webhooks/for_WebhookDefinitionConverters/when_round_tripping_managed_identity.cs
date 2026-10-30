// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Webhooks.for_WebhookDefinitionConverters;

public class when_round_tripping_managed_identity : Specification
{
    Concepts.Observation.Webhooks.WebhookDefinition _definition;
    Concepts.Observation.Webhooks.WebhookDefinition _result;

    void Establish() => _definition = new("webhook", WebhookOwner.Client, "log", [], new("https://example.com/webhook", new Concepts.Security.ManagedIdentityAuthorization("client", "api://resource/.default"), new Dictionary<string, string>()));

    void Because() => _result = _definition.ToMongoDB().ToKernel();

    [Fact] void should_preserve_the_authorization() => _result.Target.Authorization.AsT4.ShouldEqual(_definition.Target.Authorization.AsT4);
}
