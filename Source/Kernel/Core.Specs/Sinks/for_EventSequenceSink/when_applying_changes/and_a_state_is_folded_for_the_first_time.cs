// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Sinks.for_EventSequenceSink.when_applying_changes;

public class and_a_state_is_folded_for_the_first_time : given.a_sink
{
    IEnumerable<Storage.Sinks.FailedPartition> _failures;

    async Task Because() => _failures = await _sink.ApplyChanges("customer-1", Fold(new(), 10, 1, 5), 5);

    [Fact] void should_not_fail() => _failures.ShouldBeEmpty();
    [Fact] void should_publish_one_event_to_the_outbox() => _destinations.Events(Store, Tenant, EventSequenceId.Outbox).Count.ShouldEqual(1);
    [Fact] void should_not_touch_the_event_log() => _destinations.Events(Store, Tenant, EventSequenceId.Log).ShouldBeEmpty();
    [Fact] void should_publish_the_target_event_type() => _destinations.Events(Store, Tenant, EventSequenceId.Outbox)[0].Context.EventType.ShouldEqual(Target);
    [Fact] void should_publish_for_the_target_key() => _destinations.Events(Store, Tenant, EventSequenceId.Outbox)[0].Context.EventSourceId.Value.ShouldEqual("customer-1");
    [Fact] void should_carry_the_folded_state() => TotalOf(_destinations.Events(Store, Tenant, EventSequenceId.Outbox)[0].Content).ShouldEqual("10");
    [Fact] void should_carry_the_source_subject() => _destinations.Events(Store, Tenant, EventSequenceId.Outbox)[0].Context.Subject.Value.ShouldEqual("someone");
    [Fact] void should_carry_the_source_correlation() => _destinations.Events(Store, Tenant, EventSequenceId.Outbox)[0].Context.CorrelationId.Value.ShouldEqual(Guid.Parse("00000000-0000-0000-0000-000000000001"));
}
