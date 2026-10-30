// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Generations.for_GenerationInitializationStorage.when_reading_empty;

[Collection(PostgreSqlCollection.Name)]
public class on_postgresql(PostgreSqlFixture fixture) : given.a_postgresql_initialization_storage(fixture)
{
    IReadOnlyList<GenerationInitializationReceipt> _receipts;
    IReadOnlyList<GenerationInitializationBlocker> _blockers;

    async Task Because()
    {
        _receipts = await _audit.GetReceipts();
        _blockers = await _audit.GetBlockers();
    }
    [Fact] void should_have_no_receipts() => _receipts.Count.ShouldEqual(0);
    [Fact] void should_have_no_blockers() => _blockers.Count.ShouldEqual(0);
}
