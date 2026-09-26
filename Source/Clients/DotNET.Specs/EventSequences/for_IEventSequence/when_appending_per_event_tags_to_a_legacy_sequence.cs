// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_IEventSequence;

public class when_appending_per_event_tags_to_a_legacy_sequence : given.a_legacy_event_sequence
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _sequence.AppendMany(
        [new EventForEventSourceId(EventSourceId.New(), "event") { NamedTags = [new("name", "value")] }], []));

    [Fact] void should_throw_a_dedicated_exception() => _error.ShouldBeOfExactType<NamedTagsNotSupported>();
    [Fact] void should_not_call_the_legacy_implementation() => _implementation.RoutedBatchCalls.ShouldEqual(0);
}
