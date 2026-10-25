// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.when_performing;

public class and_duplicate_claim_samples_exceed_the_cap : for_ReindexConstraintsStep.given.a_closing_constraint_reindex_step
{
    UniqueConstraintDefinition _unique;
    ReindexConstraintsStepState _reported;

    void Establish()
    {
        _unique = new("UniqueEmail", [new("Registered", ["email"])]);
        _definitions.Add(_unique);
        _state.Changes = [new(_unique.Name, true, [ConstraintChangeType.EventAdded])];
        _uniqueStorage.Save(Arg.Any<EventSourceId>(), _unique, Arg.Any<EventSequenceNumber>(), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>())
            .Returns(Task.FromException(new DuplicateUniqueConstraintValue(_unique.Name, "duplicate")));
        for (ulong sequenceNumber = 0; sequenceNumber < 101; sequenceNumber++)
        {
            _events.Add(Event("Registered", sequenceNumber, Payload("email", "shared@example.com")));
        }
    }

    async Task Because()
    {
        await Perform();
        _reported = await _step.GetState();
    }

    [Fact] void should_complete_the_scan()
    {
        _result.TryGetResult(out var result).ShouldBeTrue();
        result.TryGetError(out _).ShouldBeFalse();
    }
    [Fact] void should_keep_the_total_count() => _reported.CollisionCounts[_unique.Name].ShouldEqual(101L);
    [Fact] void should_keep_only_one_hundred_samples() => _reported.CollisionSamples[_unique.Name].Count.ShouldEqual(100);
    [Fact] void should_count_the_sample_not_kept() => _reported.CollisionSampleOverflowCounts[_unique.Name].ShouldEqual(1L);
    [Fact] void should_keep_the_first_sequence_number() => _reported.CollisionSamples[_unique.Name][0].SequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_keep_the_last_sample_sequence_number() => _reported.CollisionSamples[_unique.Name][99].SequenceNumber.ShouldEqual(new EventSequenceNumber(99));
}
