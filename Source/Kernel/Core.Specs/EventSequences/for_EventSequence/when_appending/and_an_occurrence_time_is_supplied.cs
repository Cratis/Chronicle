// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class and_an_occurrence_time_is_supplied : given.an_event_sequence
{
    protected override TimeProvider EventTimeProvider => new given.EventClock();

    async Task Because() => await AppendAnEvent(DateTimeOffset.UnixEpoch);

    [Fact]
    void should_preserve_the_supplied_time() => _eventSequenceStorage.ReceivedCalls()
        .Single(call => call.GetMethodInfo().Name == "Append").GetArguments().OfType<DateTimeOffset>().Single().ShouldEqual(DateTimeOffset.UnixEpoch);
}
