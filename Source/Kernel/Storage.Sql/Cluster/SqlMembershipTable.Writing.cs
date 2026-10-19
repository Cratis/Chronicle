// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.Cluster;

public partial class SqlMembershipTable
{
    static Task<int> Update(ClusterDbContext context, MembershipRow row, int expectedEtag) => Rows(context, row)
        .Where(member => member.ETag == expectedEtag)
        .ExecuteUpdateAsync(update => update
            .SetProperty(member => member.SiloName, row.SiloName)
            .SetProperty(member => member.HostName, row.HostName)
            .SetProperty(member => member.Status, row.Status)
            .SetProperty(member => member.ProxyPort, row.ProxyPort)
            .SetProperty(member => member.SuspectTimes, row.SuspectTimes)
            .SetProperty(member => member.StartTime, row.StartTime)
            .SetProperty(member => member.IAmAliveTime, row.IAmAliveTime)
            .SetProperty(member => member.RoleName, row.RoleName)
            .SetProperty(member => member.UpdateZone, row.UpdateZone)
            .SetProperty(member => member.FaultZone, row.FaultZone)
            .SetProperty(member => member.ETag, row.ETag));

    async Task<bool> Write(MembershipEntry entry, string? etag, TableVersion version)
    {
        if (!int.TryParse(version.VersionEtag, CultureInfo.InvariantCulture, out var expectedVersion) || version.Version <= expectedVersion ||
            (etag is not null && !int.TryParse(etag, CultureInfo.InvariantCulture, out _)))
        {
            return false;
        }

        var expectedEtag = etag is null ? -1 : int.Parse(etag, CultureInfo.InvariantCulture);
        if (expectedEtag == int.MaxValue)
        {
            return false;
        }

        var row = entry.ToSql(_clusterId, expectedEtag + 1);
        await using var scope = await database.Cluster();
        var context = scope.DbContext;
        try
        {
            await context.Database.CreateExecutionStrategy().ExecuteInTransactionAsync(
                async () =>
                {
                    var changed = await context.Set<MembershipVersion>().Where(member => member.ClusterId == _clusterId && member.Version == expectedVersion)
                        .ExecuteUpdateAsync(update => update.SetProperty(member => member.Version, version.Version).SetProperty(member => member.Timestamp, DateTime.UtcNow));
                    if (changed != 1)
                    {
                        throw new DBConcurrencyException();
                    }

                    changed = etag is null ? await MembershipSql.Insert(context, row) : await Update(context, row, expectedEtag);
                    if (changed != 1)
                    {
                        throw new DBConcurrencyException();
                    }
                },
                () => Verify(context, row, version.Version),
                IsolationLevel.ReadCommitted);
            return true;
        }
        catch (DBConcurrencyException)
        {
            return false;
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            return false;
        }
        catch (PostgresException exception) when (string.Equals(exception.SqlState, PostgresErrorCodes.UniqueViolation, StringComparison.Ordinal))
        {
            return false;
        }
    }

    async Task<bool> Verify(ClusterDbContext context, MembershipRow row, int version)
    {
        var currentVersion = await context.Set<MembershipVersion>().AsNoTracking().SingleOrDefaultAsync(member => member.ClusterId == _clusterId);
        var current = await Rows(context, row).AsNoTracking().SingleOrDefaultAsync();
        return currentVersion?.Version == version && current == row;
    }
}
