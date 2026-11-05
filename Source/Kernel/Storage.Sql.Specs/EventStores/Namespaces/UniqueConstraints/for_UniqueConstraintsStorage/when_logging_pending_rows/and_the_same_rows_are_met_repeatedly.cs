// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.when_logging_pending_rows;

[Collection(SqlServerCollection.Name)]
public class and_the_same_rows_are_met_repeatedly(SqlServerFixture fixture) : given.a_server_claims_storage(fixture)
{
    given.storage_warnings _warnings;

    async Task Establish()
    {
        _warnings = new();
        await Prepare(false, _warnings, $"store-{Guid.NewGuid():N}");
        await CreateLegacySourceTable(_sourceTable);
        await AddLegacyClaim(_sourceTable, "Alice", "claim-value-9");
        await EnsureTables();
    }

    async Task Because()
    {
        await _storage.Save("alice", _perSource, 2UL, "two");
        await _storage.Save("alice", _perSource, 3UL, "three");
        await _storage.Remove("alice", _perSource);
    }

    [Fact] void should_warn_once() => _warnings.Warnings.Count.ShouldEqual(1);
    [Fact] void should_report_the_number_of_rows() => _warnings.Warnings[0].ShouldContain("1 unique constraint rows");
    [Fact] void should_not_contain_an_owner() => _warnings.Warnings[0].ToLowerInvariant().ShouldNotContain("alice");
    [Fact] void should_not_contain_a_value() => _warnings.Warnings[0].ShouldNotContain("claim-value-9");
}
