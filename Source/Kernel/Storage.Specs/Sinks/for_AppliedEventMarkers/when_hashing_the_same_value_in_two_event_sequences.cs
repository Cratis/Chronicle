// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_AppliedEventMarkers;

public class when_hashing_the_same_value_in_two_event_sequences : Specification
{
    string _first;
    string _second;

    void Because()
    {
        _first = AppliedEventMarkers.StreamHash("ns", "event-log", "source");
        _second = AppliedEventMarkers.StreamHash("ns", "outbox", "source");
    }

    [Fact] void should_differ() => _first.ShouldNotEqual(_second);
}
