// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_a_fragmented_backlog;

/// <summary>
/// A live observer whose share of one dispatch would take more sequential per-partition deliveries than the limit is moved
/// to its catch-up path before anything of that dispatch is delivered to it. Catch-up is parallel per partition and page
/// based, so it processes the same events far faster than one delivery round trip per event, and it loses nothing because
/// the events are already durable in the log.
/// </summary>
public class and_an_observer_exceeds_the_partition_run_limit : given.a_fragmented_backlog
{
    async Task Because() => await EnqueueAndDeplete(Alternating(Limit + 1, _eventType));

    [Fact] void should_not_hand_any_event_to_the_observer() => _observer.DidNotReceive().Handle(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>());
    [Fact] void should_spill_the_observer_to_catchup() => _observer.Received(1).CatchUp();
    [Fact] async Task should_remove_its_subscription() => (await _queue.GetSubscriptions()).ShouldBeEmpty();
    [Fact] void should_log_the_spill_with_its_reason() => _recordedLogs.Messages(LogLevel.Debug).Any(message => message.Contains("partition runs", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_meter_the_spill_as_a_fragmented_backlog() => _spillRecorder.Reasons.SequenceEqual(["FragmentedBacklog"]).ShouldBeTrue();
}
