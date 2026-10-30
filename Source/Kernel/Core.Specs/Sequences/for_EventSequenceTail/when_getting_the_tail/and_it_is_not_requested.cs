// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_EventSequenceTail.when_getting_the_tail;

public class and_it_is_not_requested : given.a_tail_reader
{
    EventSequenceTail _result;

    async Task Because() => _result = await EventSequenceTail.TailSequenceNumber(_storage, "store", "namespace", "log");

    [Fact] void should_return_the_tail() => _result.SequenceNumber.Value.ShouldEqual(10UL);
    [Fact] void should_not_return_an_epoch() => _result.MutationEpoch.ShouldBeNull();
    [Fact] void should_not_read_the_state() => _eventSequence.DidNotReceive().GetState();
}
