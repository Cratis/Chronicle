// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookAdmission.when_writing;

public class with_managed_identity : given.an_envelope_write
{
    void Establish() => _authorization = new ManagedIdentityAuthorization("client", "scope");

    async Task Because() => _error = await Catch.Exception(Write);

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_append_managed_identity_after_its_own_add() => _appended.Select(_ => _.GetType()).ShouldEqual([typeof(WebhookAdded), typeof(ManagedIdentityAuthorizationSetForWebhook)]);
    [Fact] void should_not_fence_authorization_against_its_own_add() => UnscopedAppendCount.ShouldEqual(1);
}
