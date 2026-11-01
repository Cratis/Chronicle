// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableOwnership.when_reading;

public class and_a_history_table_is_missing : given.persisted_metadata
{
    Exception _error;
    ReadModelTableClaims _read;

    async Task Establish() => await _context.Database.ExecuteSqlRawAsync("DROP TABLE ReplayedReadModels");

    async Task Because() => _error = await Catch.Exception(async () => _read = await _ownership.Read());

    [Fact] void should_not_fail_when_the_missing_table_is_visible() => _error.ShouldBeNull();
    [Fact] void should_report_the_scope_as_known() => _read.Unknown.ShouldBeEmpty();
    [Fact] void should_still_report_the_registered_owner() => _read.Known.Any(claim => claim.Registered).ShouldBeTrue();
}
