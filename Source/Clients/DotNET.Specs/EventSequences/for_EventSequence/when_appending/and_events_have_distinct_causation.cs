// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Auditing;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class and_events_have_distinct_causation : given.an_event_sequence_with_metadata
{
    Causation _ambient;
    Causation _first;
    Causation _second;

    void Establish()
    {
        _ambient = new(DateTimeOffset.UnixEpoch, "ambient", new Dictionary<string, string>());
        _first = new(DateTimeOffset.UnixEpoch, "first", new Dictionary<string, string>());
        _second = new(DateTimeOffset.UnixEpoch, "second", new Dictionary<string, string>());
        _causationManager.GetCurrentChain().Returns(ImmutableList.Create(_ambient));
    }

    async Task Because() => await _eventSequence.AppendMany([
        new(_source, "first", _first),
        new(_source, "second", _second)
    ]);

    [Fact] void should_send_ambient_causation_for_the_batch() => _batchRequest.Causation.ToClient().ShouldEqual([_ambient]);
    [Fact] void should_notify_first_event_with_its_causation_after_ambient() => _notifications[0].Event.Context.Causation.ShouldEqual([_ambient, _first]);
    [Fact] void should_notify_second_event_with_its_causation_after_ambient() => _notifications[1].Event.Context.Causation.ShouldEqual([_ambient, _second]);
}
