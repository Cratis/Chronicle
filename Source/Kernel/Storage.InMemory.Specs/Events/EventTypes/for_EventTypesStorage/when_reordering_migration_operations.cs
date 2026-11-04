// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes.for_EventTypesStorage;

public class when_reordering_migration_operations : given.a_populated_migration_chain
{
    IEnumerable<EventTypeId> _changed;

    async Task Establish()
    {
        var bridge = _chain.Migrations.First() with { Operations = [new("rename", new Details("first")), new("rename", new Details("second"))] };
        _chain = _chain with { Migrations = [bridge, _chain.Migrations.Last()] };
        await RegisterBatch(_chain);
    }

    async Task Because()
    {
        var bridge = _chain.Migrations.First();
        _changed = await RegisterBatch(_stale with { Migrations = [bridge with { Operations = bridge.Operations.Reverse().ToArray() }] });
    }

    [Fact] void should_treat_operation_order_as_payload() => _changed.ShouldContainOnly(_chain.Id);

    record Details(string Property) : IEventTypeMigrationOperationDetails;
}
