// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics;
using Cratis.Chronicle.Storage.Sql.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.for_IntegrationTimings;

// Synthetic EF diagnostic payloads: no connection, command or database is ever created or opened.
static class CommandEvents
{
    public const string PrivateSql = "SELECT private_query_field";

    public static DbContext NewContext() => new(new DbContextOptions<DbContext>());

    public static SqlCommandTimings NewObserver(List<string> lines, out IntegrationTimings timings)
    {
        timings = new IntegrationTimings(lines.Add, () => 0, 1000, "spec");
        return new SqlCommandTimings(timings, true);
    }

    public static KeyValuePair<string, object?> Executing(DbContext context, Guid command) =>
        new(RelationalEventId.CommandExecuting.Name, new CommandEventData(
            null!, null!, null!, null!, PrivateSql, context, DbCommandMethod.ExecuteNonQuery, command, Guid.NewGuid(), false, false, DateTimeOffset.UtcNow, CommandSource.Unknown));

    public static KeyValuePair<string, object?> Executed(DbContext context, Guid command, double milliseconds) =>
        new(RelationalEventId.CommandExecuted.Name, new CommandExecutedEventData(
            null!, null!, null!, null!, PrivateSql, context, DbCommandMethod.ExecuteNonQuery, command, Guid.NewGuid(), 1, false, false, DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(milliseconds), CommandSource.Unknown));

    public static KeyValuePair<string, object?> Failed(DbContext context, Guid command, double milliseconds) =>
        new(RelationalEventId.CommandError.Name, new CommandErrorEventData(
            null!, null!, null!, null!, PrivateSql, context, DbCommandMethod.ExecuteNonQuery, command, Guid.NewGuid(), new InvalidOperationException(), false, false, DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(milliseconds), CommandSource.Unknown));
}
