// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookAdmission.when_writing;

public class a_new_envelope_with_oauth_scope : given.an_envelope_write
{
    void Establish() => _authorization = new OAuthAuthorization("https://example.com", "client", "secret") { Scope = "scope" };

    async Task Because() => _error = await Catch.Exception(Write);

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_append_the_pair_in_order() => ((IEnumerable<EventToAppend>)_eventSequence.ReceivedCalls().Single(_ => _.GetMethodInfo().Name == "AppendMany").GetArguments()[0]!).Select(_ => _.EventType.Id).ShouldEqual([typeof(OAuthAuthorizationSetForWebhook).GetEventType().Id, typeof(OAuthScopeSetForWebhook).GetEventType().Id]);
    [Fact] void should_not_fence_the_pair_against_its_own_add() => ((ConcurrencyScopes)_eventSequence.ReceivedCalls().Single(_ => _.GetMethodInfo().Name == "AppendMany").GetArguments()[4]!).Scopes.ShouldBeEmpty();
}
