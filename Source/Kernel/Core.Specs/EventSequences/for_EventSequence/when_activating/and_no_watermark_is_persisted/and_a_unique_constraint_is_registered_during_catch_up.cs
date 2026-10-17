// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_activating.and_no_watermark_is_persisted;

public class and_a_unique_constraint_is_registered_during_catch_up : given.a_catching_up_sequence
{
    UniqueConstraintDefinition _newDefinition;

    async Task Establish()
    {
        await Seed(0, "original", email: "a");
        await Seed(1, "new", new("NewClaim", EventTypeGeneration.First), "historical");
        _newDefinition = new("registered-later", [new("NewClaim", ["email"])]);
        _registeredConstraints.Add(_newDefinition);
        _currentValidation = new ConstraintValidation([new UniqueConstraintValidator(_uniqueDefinition, _unique), new UniqueConstraintValidator(_otherDefinition, _unique), new UniqueConstraintValidator(_newDefinition, _unique)]);
        _eventType = new("Unrelated", EventTypeGeneration.First);
        await AppendAnEvent();
        _eventType = new("NewClaim", EventTypeGeneration.First);
        _eventSourceId = "live-new";
        await AppendAnEvent();
    }

    Task Because() => Tick();

    [Fact] async Task should_not_retroactively_claim_history_for_the_new_definition() => (await _claims.GetFor("new", _newDefinition)).ShouldBeEmpty();
    [Fact] async Task should_cover_live_claims_from_registration() => (await _claims.GetFor("live-new", _newDefinition)).ShouldContainOnly(Hash("claimed@example.com"));
    [Fact] async Task should_still_catch_up_the_original_definition() => (await Values("original")).ShouldContainOnly(Hash("a"));
    [Fact] void should_finish_the_original_catch_up() => _stateStorage.State.ConstraintIndexCatchUpBelow.ShouldBeNull();
}
