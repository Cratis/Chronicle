// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookAdmission.when_writing;

public class with_oauth_without_scope : given.an_envelope_write
{
    void Establish() => _authorization = new OAuthAuthorization("https://example.com", "client", "secret");

    async Task Because() => _error = await Catch.Exception(Write);

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_append_only_the_authorization_after_its_own_add() => _appended.Select(_ => _.GetType()).ShouldEqual([typeof(WebhookAdded), typeof(OAuthAuthorizationSetForWebhook)]);
    [Fact] void should_not_append_a_scope_pair() => AppendManyCount.ShouldEqual(0);
    [Fact] void should_not_fence_the_authorization_against_its_own_add() => UnscopedAppendCount.ShouldEqual(1);
}
