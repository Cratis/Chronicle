// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookDefinitionConverters;

public class when_converting_system_assigned_identity : Specification
{
    Concepts.Observation.Webhooks.WebhookTarget _result;

    void Because() => _result = new Contracts.Observation.Webhooks.WebhookTarget
    {
        ManagedIdentity = new Contracts.Security.ManagedIdentityAuthorization { Scope = "api://receiver/.default" }
    }.ToChronicle().ToContract().ToChronicle();

    [Fact] void should_preserve_the_system_assigned_identity() => _result.Authorization.AsT4.ClientId.ShouldEqual(ManagedIdentityClientId.NotSet);
}
