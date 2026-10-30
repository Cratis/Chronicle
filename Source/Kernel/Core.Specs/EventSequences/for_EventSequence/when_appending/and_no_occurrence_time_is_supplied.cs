// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class and_no_occurrence_time_is_supplied : given.an_event_sequence
{
    protected override TimeProvider EventTimeProvider => new given.EventClock();

    async Task Because() => await AppendAnEvent();

    [Fact]
    void should_use_the_injected_clock() => _eventSequenceStorage.ReceivedCalls()
        .Single(call => call.GetMethodInfo().Name == "Append").GetArguments().OfType<DateTimeOffset>().Single().ShouldEqual(given.EventClock.Now);
}
