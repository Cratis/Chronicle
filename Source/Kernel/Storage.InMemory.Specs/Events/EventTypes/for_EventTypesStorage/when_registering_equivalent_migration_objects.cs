// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes.for_EventTypesStorage;

public class when_registering_equivalent_migration_objects : given.a_populated_migration_chain
{
    IEnumerable<EventTypeId> _changed;

    async Task Because()
    {
        var equivalent = _chain with { Migrations = _chain.Migrations.Reverse().Select(_ => _ with { UpcastJmesPath = _.UpcastJmesPath.DeepClone().AsObject(), DowncastJmesPath = _.DowncastJmesPath.DeepClone().AsObject() }).ToArray() };
        _changed = await RegisterBatch(equivalent);
    }

    [Fact] void should_not_report_a_change_for_new_objects_or_list_order() => _changed.ShouldBeEmpty();
}
