// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Storage.InMemory;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_a_poison_settlement_survives_replay_replacement : given.a_replay
{
    readonly ConstraintIndexPoisonTracker _tracker = new(new InMemoryStorageErrorClassifier());
    int _poisonReads;

    void Establish()
    {
        Event(0, "healthy", "Claimed", "first");
        Event(1, "poisoned", "Poisoned", "second");
        _definition = _definition with { EventDefinitions = [new("Claimed", ["email"]), new("Poisoned", ["email"])] };
        _types.GetFor((EventTypeId)"Poisoned", Arg.Any<EventTypeGeneration?>()).Returns(_ =>
        {
            _poisonReads++;
            return Task.FromException<EventTypeSchema>(new FormatException("missing schema"));
        });
    }

    async Task Because()
    {
        var failures = new Dictionary<ConstraintName, ConstraintIndexReplayFailure> { [_definition.Name] = new((EventSequenceNumber)1, new FormatException("missing schema")) };
        for (var attempt = 0; attempt < 5; attempt++) _tracker.Observe(failures, true);

        // A fresh replay represents either a hard deadline or definition synchronization discarding the old fold.
        _replay = new(_sequence, _unique, _closureStorage, _content, Substitute.For<ILogger<ConstraintIndexReplay>>(), [new(_definition, EventSequenceNumber.First)], 2)
        {
            SettledPoison = _tracker.Settled
        };
        _result = await _replay.Continue();
    }

    [Fact] void should_not_retry_the_settled_poison_event() => _poisonReads.ShouldEqual(0);
    [Fact] void should_complete_without_another_five_failures() => _result.IsCompleted.ShouldBeTrue();
    [Fact] async Task should_preserve_the_preceding_valid_event() => (await Values("healthy")).ShouldContain(Hash("first").Value);
}
