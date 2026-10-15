// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_activating.and_no_watermark_is_persisted;

public class and_the_boundary_is_raised_after_differences_are_collected : given.a_catching_up_sequence
{
    bool _expire = true;
    EventSequenceNumber? _raisedBoundary;
    int _initialWrites;
    bool _persistedBeforeLiveRemoval;

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
        _initialWrites = _claimWrites;
        _eventSequence.ConstraintIndexCatchUpKeyLimit = 1;
        _snapshots.BeforeWrite = () =>
        {
            _persistedBeforeLiveRemoval |= !_unique.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "Remove" && call.GetArguments()[0] is EventSourceId source && source.Value == "third");
            return Task.CompletedTask;
        };
    }

    async Task Because()
    {
        _eventSourceId = "third";
        _eventType = new("Removed", EventTypeGeneration.First);
        await AppendAnEvent();
        await Tick();
        _raisedBoundary = _snapshots.Snapshots.Last(snapshot => snapshot.ConstraintIndexCatchUpBelow is not null).ConstraintIndexCatchUpBelow;
    }

    [Fact] void should_have_yielded_with_unwritten_collected_differences() => _initialWrites.ShouldEqual(1);
    [Fact] void should_not_delay_live_updates_for_the_boundary_snapshot() => _persistedBeforeLiveRemoval.ShouldBeFalse();
    [Fact] void should_persist_the_new_boundary_before_resuming_catch_up() => _raisedBoundary.ShouldEqual((EventSequenceNumber)4);
    [Fact] async Task should_not_write_the_old_collected_claim() => (await Values("third")).ShouldBeEmpty();
    [Fact] async Task should_still_repair_other_unwritten_keys() => (await Values("second")).ShouldContainOnly(Hash("b"));
    [Fact] void should_not_rewrite_already_correct_keys() => _claimWrites.ShouldEqual(2);
}
