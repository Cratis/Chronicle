// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_activating.and_no_watermark_is_persisted;

public class and_the_catch_up_was_interrupted : given.a_catching_up_sequence
{
    bool _expire = true;
    int _writesBeforeRestart;

    async Task Establish()
    {
        await Seed(0, "first", email: "a");
        await Seed(1, "second", email: "b");
        await Seed(2, "third", email: "c");
        _unique.Save(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<EventSequenceNumber>(), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                _claimWrites++;
                await _claims.Save(call.ArgAt<EventSourceId>(0), call.ArgAt<UniqueConstraintDefinition>(1), call.ArgAt<EventSequenceNumber>(2), call.ArgAt<UniqueConstraintValue>(3), call.ArgAt<string>(4));
                if (_expire) _clock.Advance(TimeSpan.FromSeconds(2));
                _expire = false;
            });
        await Tick();
        _writesBeforeRestart = _claimWrites;
        await Reactivate();
    }

    Task Because() => Tick();

    [Fact] void should_have_interrupted_after_one_complete_group() => _writesBeforeRestart.ShouldEqual(1);
    [Fact] void should_not_rewrite_an_already_correct_key() => _claimWrites.ShouldEqual(3);
    [Fact] async Task should_finish_the_remaining_claims() => (await Values("third")).ShouldContainOnly(Hash("c"));
    [Fact] void should_persist_completion() => _snapshots.Written!.ConstraintIndexCatchUpBelow.ShouldBeNull();
}
