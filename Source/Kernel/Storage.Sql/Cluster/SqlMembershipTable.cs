// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Orleans.Configuration;

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Stores Orleans membership with atomic cluster-version and per-silo ETag comparisons.
/// </summary>
/// <param name="database">The migration-aware cluster database.</param>
/// <param name="options">The Orleans cluster identity.</param>
public partial class SqlMembershipTable(IDatabase database, IOptions<ClusterOptions> options) : IMembershipTable
{
    readonly string _clusterId = options.Value.ClusterId;

    /// <inheritdoc/>
    public async Task InitializeMembershipTable(bool tryInitTableVersion)
    {
        await using var scope = await database.Cluster();
        var context = scope.DbContext;
        if (tryInitTableVersion)
        {
            await context.Database.CreateExecutionStrategy().ExecuteInTransactionAsync(
                () => MembershipSql.Initialize(context, _clusterId),
                () => context.Set<MembershipVersion>().AnyAsync(row => row.ClusterId == _clusterId),
                IsolationLevel.ReadCommitted);
        }
    }

    /// <inheritdoc/>
    public async Task<MembershipTableData> ReadAll()
    {
        await using var scope = await database.Cluster();
        return await Read(scope.DbContext, null);
    }

    /// <inheritdoc/>
    public async Task<MembershipTableData> ReadRow(SiloAddress key)
    {
        await using var scope = await database.Cluster();
        return await Read(scope.DbContext, key);
    }

    /// <inheritdoc/>
    public Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion) => Write(entry, null, tableVersion);

    /// <inheritdoc/>
    public Task<bool> UpdateRow(MembershipEntry entry, string etag, TableVersion tableVersion) => Write(entry, etag, tableVersion);

    /// <inheritdoc/>
    public async Task UpdateIAmAlive(MembershipEntry entry)
    {
        await using var scope = await database.Cluster();
        var row = entry.ToSql(_clusterId, 0);
        await Rows(scope.DbContext, row).ExecuteUpdateAsync(update => update.SetProperty(member => member.IAmAliveTime, row.IAmAliveTime));
    }

    /// <inheritdoc/>
    public async Task CleanupDefunctSiloEntries(DateTimeOffset beforeDate)
    {
        await using var scope = await database.Cluster();
        await scope.DbContext.Set<MembershipRow>()
            .Where(row => row.ClusterId == _clusterId && row.Status != (int)SiloStatus.Active && row.IAmAliveTime < beforeDate.UtcDateTime)
            .ExecuteDeleteAsync();
    }

    /// <inheritdoc/>
    public async Task DeleteMembershipTableEntries(string clusterId)
    {
        await using var scope = await database.Cluster();
        var context = scope.DbContext;
        await context.Database.CreateExecutionStrategy().ExecuteInTransactionAsync(
            () => context.Set<MembershipVersion>().Where(row => row.ClusterId == clusterId).ExecuteDeleteAsync(),
            async () => !await context.Set<MembershipVersion>().AnyAsync(row => row.ClusterId == clusterId),
            IsolationLevel.ReadCommitted);
    }

    static IQueryable<MembershipRow> Rows(ClusterDbContext context, MembershipRow row) => context.Set<MembershipRow>()
        .Where(member => member.ClusterId == row.ClusterId && member.Address == row.Address && member.Port == row.Port && member.Generation == row.Generation);

    async Task<MembershipTableData> Read(ClusterDbContext context, SiloAddress? address)
    {
        // The version MUST precede the rows: a concurrent writer then invalidates this view's CAS.
        await context.Database.OpenConnectionAsync();
        var version = await context.Set<MembershipVersion>().AsNoTracking().SingleOrDefaultAsync(row => row.ClusterId == _clusterId);
        if (version is null)
        {
            return new MembershipTableData(new TableVersion(0, "0"));
        }

        var query = context.Set<MembershipRow>().AsNoTracking().Where(row => row.ClusterId == _clusterId);
        if (address is not null)
        {
            var ip = address.Endpoint.Address.ToString();
            query = query.Where(row => row.Address == ip && row.Port == address.Endpoint.Port && row.Generation == address.Generation);
        }

        var rows = await query.ToListAsync();
        var members = rows.ConvertAll(row => Tuple.Create(row.ToOrleans(), row.ETag.ToString(CultureInfo.InvariantCulture)));
        return new MembershipTableData(members, new TableVersion(version.Version, version.Version.ToString(CultureInfo.InvariantCulture)));
    }
}
