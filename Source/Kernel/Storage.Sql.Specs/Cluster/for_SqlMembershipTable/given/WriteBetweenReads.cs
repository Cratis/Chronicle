// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.given;

public sealed class WriteBetweenReads(Func<Task> write) : DbCommandInterceptor
{
    bool _written;

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        if (!_written && command.CommandText.Contains(WellKnownTableNames.MembershipEntries, StringComparison.Ordinal))
        {
            _written = true;
            await write();
        }

        return result;
    }
}
