// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.given;

public class a_reindex_with_duplicate_claims : a_closing_constraint_reindex_step
{
    protected UniqueConstraintDefinition _unique;
    protected Storage.InMemory.Events.Constraints.UniqueConstraintsStorage _claims;
    protected UniqueConstraintValue _hash;

    void Establish()
    {
        _unique = new("UniqueEmail", [new("Registered", ["email"])]);
        _definitions.Add(_unique);
        _state.Changes = [new(_unique.Name, true, [ConstraintChangeType.EventAdded])];
        _claims = new();
        _hash = new List<UniqueConstraintPropertyAndValue> { new("email", "shared@example.com") }.GetValue();
        _uniqueStorage.ClearValues(_unique, Arg.Any<string>()).Returns(call => _claims.ClearValues(_unique, call.Arg<string>()));
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
        _events.Add(Event("Registered", 0, Payload("email", "shared@example.com")));
        var duplicate = Event("Registered", 1, Payload("email", "shared@example.com"));
        _events.Add(duplicate with { Context = duplicate.Context with { EventSourceId = "duplicate-one" } });
        _events.Add(duplicate with { Context = duplicate.Context with { EventSourceId = "duplicate-two", SequenceNumber = 2 } });
        var following = Event("Registered", 3, Payload("email", "different@example.com"));
        _events.Add(following with { Context = following.Context with { EventSourceId = "following" } });
    }
}
