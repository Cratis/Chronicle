// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

using context = Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.when_appending_with_an_idempotency_key.and_two_appends_race_for_the_key.context;

namespace Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.when_appending_with_an_idempotency_key;

[Collection(ChronicleCollection.Name)]
public class and_two_appends_race_for_the_key(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_log_with_idempotency_keys(fixture)
    {
        async Task Because()
        {
            Racing = await Task.WhenAll(Append(), Append());
            await CountEvents();
        }
    }

    [Fact] void should_settle_both_attempts() => Context.Racing.All(result => result.IsSuccess).ShouldBeTrue();
    [Fact] void should_append_exactly_once() => Context.Racing.Count(result => result.Outcome == AppendOutcome.Appended).ShouldEqual(1);
    [Fact] void should_settle_the_other_attempt_as_already_appended() => Context.Racing.Count(result => result.Outcome == AppendOutcome.AlreadyAppended).ShouldEqual(1);
    [Fact] void should_return_the_same_sequence_number() => Context.Racing[0].Result.SequenceNumber.ShouldEqual(Context.Racing[1].Result.SequenceNumber);
    [Fact] void should_store_one_event() => Context.Count.ShouldEqual(1UL);
}
