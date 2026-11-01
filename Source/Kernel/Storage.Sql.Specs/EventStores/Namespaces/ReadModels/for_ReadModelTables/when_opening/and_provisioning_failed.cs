// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTables.when_opening;

public class and_provisioning_failed : given.a_sqlite_placement
{
    Exception? _firstError;
    Exception? _retryError;
    int _ledgerOpens;

    async Task Establish()
    {
        _placements.FailNextOpen(new InvalidOperationException("fixture failure"));
        _firstError = await Catch.Exception(async () =>
        {
            await using var scope = await _tables.Open(_live, _columns);
        });
        _ledgerOpens = _placements.LedgerOpens;
    }

    async Task Because()
    {
        _retryError = await Catch.Exception(async () =>
        {
            await using var scope = await _tables.Open(_live, _columns);
        });
    }

    [Fact] void should_return_the_original_failure() => _firstError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_retry_resolution_instead_of_caching_failure() => _placements.LedgerOpens.ShouldEqual(_ledgerOpens + 1);
    [Fact] void should_finish_provisioning_on_retry() => _retryError.ShouldBeNull();
}
