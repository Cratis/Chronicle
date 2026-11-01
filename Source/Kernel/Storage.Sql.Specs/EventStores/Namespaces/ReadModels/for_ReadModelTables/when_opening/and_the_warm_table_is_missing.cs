// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTables.when_opening;

public class and_the_warm_table_is_missing : given.a_sqlite_placement
{
    int _ledgerOpens;
    Exception? _error;

    async Task Establish()
    {
        await using var scope = await _tables.Open(_live, _columns);
        _placements.FailNextOpen(new ReadModelTableMissing(scope.DbContext.TableName));
        _ledgerOpens = _placements.LedgerOpens;
    }

    async Task Because()
    {
        _error = await Catch.Exception(async () =>
        {
            await using var scope = await _tables.Open(_live, _columns);
        });
    }

    [Fact] void should_resolve_again_once() => _placements.LedgerOpens.ShouldEqual(_ledgerOpens + 1);
    [Fact] void should_use_the_same_binding() => _error.ShouldBeNull();
}
