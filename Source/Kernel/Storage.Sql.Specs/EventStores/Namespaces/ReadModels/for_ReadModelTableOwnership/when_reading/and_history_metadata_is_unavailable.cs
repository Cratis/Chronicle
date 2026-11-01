// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableOwnership.when_reading;

public class and_history_metadata_is_unavailable : given.persisted_metadata
{
    Exception _error;

    async Task Establish() => await _context.Database.ExecuteSqlRawAsync("DROP TABLE ReplayedReadModels");

    async Task Because() => _error = await Catch.Exception(_ownership.Read);

    [Fact] void should_fail_instead_of_reporting_an_empty_claimant_set() => _error.ShouldBeOfExactType<SqliteException>();
}
