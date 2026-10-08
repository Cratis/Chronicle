// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverFiltersExtensions.when_applying;

public class and_some_events_do_not_pass_the_filters : Specification
{
    AppendedEvent[] _result;

    static AppendedEvent CreateEvent(EventSequenceNumber sequenceNumber, params Tag[] tags) =>
        AppendedEvent.EmptyWithEventSequenceNumber(sequenceNumber) with
        {
            Context = EventContext.Empty with { SequenceNumber = sequenceNumber, Tags = tags }
        };

    void Because() => _result = new ObserverFilters(["audited"]).Apply(
    [
        CreateEvent(1UL, new Tag("audited")),
        CreateEvent(2UL),
        CreateEvent(3UL, new Tag("other")),
        CreateEvent(4UL, new Tag("audited"))
    ]);

    [Fact] void should_keep_only_the_events_that_pass_in_their_order() => _result.Select(_ => _.Context.SequenceNumber).ToArray().ShouldEqual([1UL, 4UL]);
}
