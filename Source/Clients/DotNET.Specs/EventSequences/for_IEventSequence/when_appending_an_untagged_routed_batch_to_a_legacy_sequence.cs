// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_IEventSequence;

public class when_appending_an_untagged_routed_batch_to_a_legacy_sequence : given.a_legacy_event_sequence
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _sequence.AppendMany([new EventForEventSourceId(EventSourceId.New(), "event")], []));

    [Fact] void should_delegate_to_the_legacy_implementation() => _implementation.RoutedBatchCalls.ShouldEqual(1);
    [Fact] void should_not_fail() => _error.ShouldBeNull();
}
