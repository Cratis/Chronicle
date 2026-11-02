// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookAdmission.when_writing;

public class a_new_envelope_webhook : given.an_envelope_write
{
    void Establish() => _authorization = new BearerTokenAuthorization("token");

    async Task Because() => _error = await Catch.Exception(Write);

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_build_changes_once() => _comparisons.ShouldEqual(1);
    [Fact] void should_fence_against_no_lifecycle() => ScopedAppends.Single().SequenceNumber.ShouldEqual(EventSequenceNumber.BeforeFirst);
    [Fact] void should_scope_to_the_event_source() => ScopedAppends.Single().EventSourceId.ShouldBeTrue();
    [Fact] void should_scope_to_both_lifecycle_events() => ScopedAppends.Single().EventTypes!.Select(_ => _.Id).ShouldContainOnly(typeof(WebhookAdded).GetEventType().Id, typeof(WebhookRemoved).GetEventType().Id);
    [Fact] void should_not_fence_authorization_against_its_own_add() => UnscopedAppendCount.ShouldEqual(1);
    [Fact] void should_append_authorization_after_the_add() => _appended.Select(_ => _.GetType()).ShouldEqual([typeof(WebhookAdded), typeof(BearerTokenAuthorizationSetForWebhook)]);
}
