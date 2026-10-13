// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.when_performing;

public class and_history_contains_duplicate_values : for_ReindexConstraintsStep.given.a_closing_constraint_reindex_step
{
    UniqueConstraintDefinition _unique;
    Storage.InMemory.Events.Constraints.UniqueConstraintsStorage _claims;
    UniqueConstraintValue _hash;

    void Establish()
    {
        _unique = new("unique", [new("Registered", ["email"])]);
        _definitions.Add(_unique);
        _state.Changes = [new("unique", true, [ConstraintChangeType.EventAdded])];
        _claims = new();
        _hash = new List<UniqueConstraintPropertyAndValue> { new("email", "shared@example.com") }.GetValue();
        _uniqueStorage.ClearValues(_unique, Arg.Any<string>()).Returns(call => _claims.ClearValues(_unique, call.Arg<string>()));

        // Default-mode InMemory does not settle claims until the next PR. Emulate a settled store's
        // duplicate signal here while retaining the real storage for the resulting claims.
        _uniqueStorage.Save(Arg.Any<EventSourceId>(), _unique, Arg.Any<EventSequenceNumber>(), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>()).Returns(async call =>
        {
            var owner = call.Arg<EventSourceId>();
            var value = call.Arg<UniqueConstraintValue>();
            var scope = call.Arg<string>();
            if (!(await _claims.IsAllowed(owner, _unique, value, scope)).IsAllowed)
            {
                throw new DuplicateUniqueConstraintValue(_unique.Name, owner);
            }

            await _claims.Save(owner, _unique, call.Arg<EventSequenceNumber>(), value, scope);
        });
        _events.Add(Event("Registered", 1, Payload("email", "shared@example.com")));
        var duplicate = Event("Registered", 2, Payload("email", "shared@example.com"));
        _events.Add(duplicate with { Context = duplicate.Context with { EventSourceId = "later" } });
        var following = Event("Registered", 3, Payload("email", "different@example.com"));
        _events.Add(following with { Context = following.Context with { EventSourceId = "following" } });
    }

    async Task Because() => await Perform();

    [Fact] void should_complete_without_an_exception() => _result.TryGetException(out _).ShouldBeFalse();
    [Fact] async Task should_keep_the_first_claimant() => (await _claims.IsAllowed("source", _unique, _hash)).IsAllowed.ShouldBeTrue();
    [Fact] async Task should_refuse_the_later_claimant() => (await _claims.IsAllowed("later", _unique, _hash)).IsAllowed.ShouldBeFalse();
    [Fact] async Task should_keep_the_first_claim_position() => (await _claims.IsAllowed("later", _unique, _hash)).SequenceNumber.ShouldEqual(new EventSequenceNumber(1));
    [Fact] async Task should_continue_past_the_duplicate() => await _uniqueStorage.Received(1).Save("following", _unique, new EventSequenceNumber(3), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>());
}
