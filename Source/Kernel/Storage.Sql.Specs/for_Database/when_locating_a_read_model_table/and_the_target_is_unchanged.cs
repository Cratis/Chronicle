// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_locating_a_read_model_table;

public class and_the_target_is_unchanged : given.a_sqlite_database
{
    ReadModelTablePlace _first;
    ReadModelTablePlace _second;

    async Task Because()
    {
        IReadModelTablePlacements placements = _database;
        _first = await placements.Locate("a", "b", ConnectionPurpose.Runtime);
        _second = await placements.Locate("a", "b", ConnectionPurpose.Runtime);
    }

    [Fact] void should_reuse_the_place() => ReferenceEquals(_second, _first).ShouldBeTrue();
}
