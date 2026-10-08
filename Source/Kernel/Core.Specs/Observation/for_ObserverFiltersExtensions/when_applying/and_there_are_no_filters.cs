// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverFiltersExtensions.when_applying;

public class and_there_are_no_filters : Specification
{
    AppendedEvent[] _events;
    AppendedEvent[] _result;

    void Establish() => _events = [AppendedEvent.EmptyWithEventSequenceNumber(1UL), AppendedEvent.EmptyWithEventSequenceNumber(2UL)];

    void Because() => _result = ((ObserverFilters?)null).Apply(_events);

    [Fact] void should_keep_every_event() => _result.ShouldEqual(_events);
}
