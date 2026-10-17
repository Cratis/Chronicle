// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_immediate_appends_are_refused;

public class without_read_protection : given.public_entries
{
    Task Because() => ExerciseAppends();

    [Fact] void should_refuse_all_ten_append_entries() => _errors.Count.ShouldEqual(10);
    [Fact] void should_refuse_before_evaluating_invalid_or_lazy_inputs() => _errors.Values.ShouldEachConformTo(error => error is ImmediateAppendRefused);
    [Fact] void should_not_reach_transport() => _sequences.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_resolve_event_types() => _eventTypes.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_serialize_or_run_additional_information_providers() => _eventSerializer.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_resolve_event_source_providers() => _eventSources.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_resolve_concurrency_strategies() => _concurrencyScopeStrategies.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_invoke_concurrency_providers() => _concurrencyScopeStrategy.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_resolve_identity() => _identityProvider.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_publish_append_notifications() => _notifications.ShouldEqual(0);
}
