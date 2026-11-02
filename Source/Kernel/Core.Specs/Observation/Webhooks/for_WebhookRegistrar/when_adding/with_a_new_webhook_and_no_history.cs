// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class with_a_new_webhook_and_no_history : given.a_pinned_registration
{
    ConcurrencyScope _scope;

    async Task Because()
    {
        _error = await Catch.Exception(() => _registrar.Add("store", [_definition]));
        _scope = ScopedAppends.Single();
    }

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_expect_no_matching_event() => _scope.SequenceNumber.ShouldEqual(EventSequenceNumber.BeforeFirst);
    [Fact] void should_scope_to_the_event_source() => _scope.EventSourceId.ShouldBeTrue();
    [Fact] void should_scope_to_the_lifecycle_events() => _scope.EventTypes!.Select(_ => _.Id).ShouldContainOnly(typeof(WebhookAdded).GetEventType().Id, typeof(WebhookRemoved).GetEventType().Id);
}
