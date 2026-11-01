// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableOwnership.when_reading;

public class and_a_scope_cannot_be_read : given.persisted_metadata
{
    ReadModelTableClaims _read;

    async Task Establish() => await _context.Database.ExecuteSqlRawAsync("DROP TABLE ReplayedReadModels; CREATE TABLE ReplayedReadModels (Unrelated TEXT NOT NULL)");

    async Task Because() => _read = await _ownership.Read();

    [Fact] void should_report_the_scope_as_unknown_not_empty() => _read.Unknown.Single().EventStoreId.ShouldEqual("store");
    [Fact] void should_name_only_the_exception_type() => _read.Unknown.Single().Reason.ShouldEqual("SqliteException");
}
