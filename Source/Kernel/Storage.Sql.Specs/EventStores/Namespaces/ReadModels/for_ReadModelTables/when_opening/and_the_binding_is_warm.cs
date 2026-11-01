// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTables.when_opening;

public class and_the_binding_is_warm : given.a_sqlite_placement
{
    int _ledgerOpens;
    int _targets;
    int _tableOpens;
    string _first;
    string _second;

    async Task Establish()
    {
        await using var scope = await _tables.Open(_live, _columns);
        _first = scope.DbContext.TableName;
        _ledgerOpens = _placements.LedgerOpens;
        _targets = _placements.Targets;
        _tableOpens = _placements.TableOpens;
    }

    async Task Because()
    {
        await using var scope = await _tables.Open(_live, _columns);
        _second = scope.DbContext.TableName;
    }

    [Fact] void should_keep_the_bound_name() => _second.ShouldEqual(_first);
    [Fact] void should_resolve_the_target_once() => _placements.Targets.ShouldEqual(_targets + 1);
    [Fact] void should_open_one_table_context() => _placements.TableOpens.ShouldEqual(_tableOpens + 1);
    [Fact] void should_not_open_the_ledger_or_enumerate_ownership() => _placements.LedgerOpens.ShouldEqual(_ledgerOpens);
}
