// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep;

public class when_reindexing_a_value_released_before_another_source_claimed_it : given.a_unique_constraint_to_reindex
{
    const string ValueHash = "4f7aa54b8d9a8f5e7b06bf38217a84dfd7272bd50f5aebe97ae321f24eceb291";
    EventSourceId _earlierOwner;
    EventSourceId _currentOwner;
    Exception? _error;

    async Task Establish()
    {
        _storage = new Storage.InMemory.Events.Constraints.UniqueConstraintsStorage();
        _definition = _definition with
        {
            Mode = UniqueConstraintMode.PerValue,
            RemovedWith = ["ValueReleased"],
            RemovalEventDefinitions = [new("ValueReleased", [Property])]
        };
        _validator = new(_definition, _storage);
        _earlierOwner = EventSourceId.New();
        _currentOwner = EventSourceId.New();
        await _storage.Save(_currentOwner, _definition, 2UL, ValueHash);
    }

    async Task Because() => _error = await Catch.Exception(async () =>
    {
        var first = EventFor(_earlierOwner);
        var released = first with { Context = first.Context with { EventType = new EventType("ValueReleased", 1), SequenceNumber = 1UL } };
        var claimedAgain = EventFor(_currentOwner);
        claimedAgain = claimedAgain with { Context = claimedAgain.Context with { SequenceNumber = 2UL } };
        foreach (var @event in new[] { first, released, claimedAgain })
        {
            await ReindexConstraintsStep.ReindexEvent(_definition, @event, ContentWith("SomeValue"), _seen, _validator, _storage);
        }
    });

    [Fact] void should_not_fail_replaying_the_earlier_claim() => _error.ShouldBeNull();
    [Fact] async Task should_reserve_the_value_for_the_current_owner() => (await _storage.IsAllowed(_currentOwner, _definition, ValueHash)).IsAllowed.ShouldBeTrue();
    [Fact] async Task should_refuse_the_earlier_owner() => (await _storage.IsAllowed(_earlierOwner, _definition, ValueHash)).IsAllowed.ShouldBeFalse();
    [Fact] async Task should_record_the_final_claim_position() => (await _storage.IsAllowed(_earlierOwner, _definition, ValueHash)).SequenceNumber.ShouldEqual((EventSequenceNumber)2UL);
    [Fact] void should_clear_the_scope_only_once() => _seen.Count.ShouldEqual(1);
}
