// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

public class and_the_occurrence_clock_is_controlled : given.appending_many_events
{
    protected override TimeProvider EventTimeProvider => new given.EventClock();

    void Establish() => _events = [EventToAppendFor("explicit") with { Occurred = DateTimeOffset.UnixEpoch }, EventToAppendFor("default")];

    async Task Because() => await _eventSequence.AppendMany(_events, CorrelationId.New(), [], Identity.System, new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));

    EventToAppendToStorage[] StoredEvents => _eventSequenceStorage.ReceivedCalls().Single(call => call.GetMethodInfo().Name == "AppendMany")
        .GetArguments().OfType<IEnumerable<EventToAppendToStorage>>().Single().ToArray();

    [Fact] void should_preserve_the_supplied_time() => StoredEvents[0].Occurred.ShouldEqual(DateTimeOffset.UnixEpoch);
    [Fact] void should_use_the_injected_clock_for_the_default() => StoredEvents[1].Occurred.ShouldEqual(given.EventClock.Now);
}
