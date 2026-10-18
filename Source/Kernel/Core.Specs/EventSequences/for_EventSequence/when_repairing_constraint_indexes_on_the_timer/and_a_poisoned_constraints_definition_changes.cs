// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_repairing_constraint_indexes_on_the_timer;

public class and_a_poisoned_constraints_definition_changes : given.an_event_sequence_with_repairable_indexes
{
    ConstraintIndexPoisonTracker _tracker;
    (ConstraintName Name, EventSequenceNumber Position)[] _settledAfterSynchronization;
    protected override EventSequenceNumber InitialNext => 2;

    async Task Establish()
    {
        await Seed(0);
        await Seed(1, type: new("OtherEvent", EventTypeGeneration.First));
        _tracker = (ConstraintIndexPoisonTracker)typeof(EventSequence).GetField("_constraintIndexPoisonTracker", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_eventSequence)!;
        var failures = new Dictionary<ConstraintName, ConstraintIndexReplayFailure>
        {
            [_uniqueDefinition.Name] = new(EventSequenceNumber.First, new FormatException("old definition")),
            [_otherDefinition.Name] = new((EventSequenceNumber)1, new FormatException("unchanged definition"))
        };
        for (var attempt = 0; attempt < 5; attempt++) _tracker.Observe(failures, true);
        _uniqueDefinition = _uniqueDefinition with { IgnoreCasing = true };
        var validation = new ConstraintValidation([new UniqueConstraintValidator(_uniqueDefinition, _unique), new UniqueConstraintValidator(_otherDefinition, _unique)]);
        typeof(EventSequence).GetField("_constraints", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_eventSequence, validation);
    }

    async Task Because()
    {
        typeof(EventSequence).GetMethod("SynchronizeIndexedDefinitions", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_eventSequence, [null]);
        _settledAfterSynchronization = _tracker.Settled.ToArray();
        await given.ConstraintRepairTimer.Tick(_silo);
    }

    [Fact] void should_forget_poison_for_the_changed_definition() => _settledAfterSynchronization.ShouldNotContain((_uniqueDefinition.Name, EventSequenceNumber.First));
    [Fact] void should_keep_poison_for_an_unchanged_definition() => _settledAfterSynchronization.ShouldContain((_otherDefinition.Name, (EventSequenceNumber)1));
    [Fact] async Task should_retry_the_event_under_the_corrected_definition() => (await _claims.GetFor(_eventSourceId, _uniqueDefinition)).ShouldNotBeEmpty();
    [Fact] async Task should_complete_the_repaired_range() => (await _log.GetState()).ConstraintIndexWatermark.ShouldEqual((EventSequenceNumber)2);
}
