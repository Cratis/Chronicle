// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.given;

public class a_handoff_reindex_step : a_closing_constraint_reindex_step
{
    protected readonly List<string> _calls = [];
    protected UniqueConstraintDefinition _unique;

    void Establish()
    {
        _unique = new("unique", [new("Registered", ["email"])]);
        _definitions.Add(_unique);
        _state.Changes = [new("unique", true, [ConstraintChangeType.EventAdded])];
        _sequence.ConstraintIndexRebuildStarting(_runId, Arg.Any<IEnumerable<ConstraintName>>()).Returns(_ =>
        {
            _calls.Add("starting");
            return Task.CompletedTask;
        });
        _uniqueStorage.ClearValues(_unique, Arg.Any<string>()).Returns(_ =>
        {
            _calls.Add("clear");
            return Task.CompletedTask;
        });
        _events.Add(Event("Registered", 0, Payload("email", "first@example.com")));
        _events.Add(Event("Registered", 1, Payload("email", "last@example.com")));
    }
}
