// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.InMemory.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.given;

public class a_filtered_and_full_scan_reindex : a_closing_constraint_reindex_step
{
    protected UniqueConstraintDefinition _unique;
    protected string[] _filtered;
    protected string[] _full;
    protected string _scope = "";

    void Establish()
    {
        _unique = new("unique", [new("Registered", ["email"])], ["Removed"]);
        _state.Changes = [new("unique", true, [ConstraintChangeType.EventAdded])];
        _events.Add(Event("Registered", 1, Payload("email", "first@example.com")));
        _events.Add(Event("Unrelated", 2));
        _events.Add(Event("Removed", 3));
        var older = Event("Registered", 4, Payload("email", "last@example.com"));
        _events.Add(older with { Context = older.Context with { EventType = new("Registered", 2) } });
    }

    protected async Task CompareScans()
    {
        _definitions.Clear();
        _definitions.Add(_unique);
        var rows = new UniqueConstraintsStorage();
        _storage.GetEventStore("store").GetNamespace("ns").GetUniqueConstraintsStorage(_state.EventSequenceId).Returns(rows);
        await Perform();
        _result.TryGetException(out _).ShouldBeFalse();
        _filtered = (await rows.GetFor("source", _unique, _scope)).Select(value => value.Value).ToArray();
        _ignoreEventTypeFilter = true;
        await Perform();
        _result.TryGetException(out _).ShouldBeFalse();
        _full = (await rows.GetFor("source", _unique, _scope)).Select(value => value.Value).ToArray();
    }
}
