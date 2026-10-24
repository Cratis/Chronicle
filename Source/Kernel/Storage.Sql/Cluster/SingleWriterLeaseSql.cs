// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Runtime.CompilerServices;
using Cratis.Chronicle.Storage.SingleWriter;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Executes database-clock lease comparisons and updates.
/// </summary>
internal static class SingleWriterLeaseSql
{
    /// <summary>
    /// Acquires or extends a lease atomically.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="claim">The claim.</param>
    /// <param name="cancellationToken">The cancellationToken.</param>
    /// <returns>The affected row count.</returns>
    internal static async Task<int> Acquire(ClusterDbContext context, SingleWriterLeaseClaim claim, CancellationToken cancellationToken)
    {
        var table = await Table(context, cancellationToken);
        var helper = context.GetService<ISqlGenerationHelper>();
        string C(string name) => helper.DelimitIdentifier(name);
        string[] names = ["Name", "Owner", "ClusterId", "ServiceId", "HostName", "Version", "AcquiredAt", "RenewedAt", "ExpiresAt"];
        var columns = string.Join(", ", names.Select(C));
        var (now, expiry) = Clock(context);
        var values = $"{{0}}, {{1}}, {{2}}, {{3}}, {{4}}, {{5}}, {now}, {now}, {expiry}";
        string Current(string column) => context.Database.IsSqlServer() ? C(column) : $"{C("lease")}.{C(column)}";
        var owner = Current("Owner");
        var acquired = C("AcquiredAt");
        var expires = C("ExpiresAt");
        var name = C("Name");
        var assignments = string.Join(", ", names.Skip(1).Take(5).Select((n, i) => $"{C(n)} = {{{i + 1}}}"));
        var times = $"{acquired} = CASE WHEN {owner} = {{1}} THEN {Current("AcquiredAt")} ELSE {now} END, {C("RenewedAt")} = {now}, {expires} = {expiry}";
        var sql = context.Database.IsSqlServer()
            ? $"UPDATE {table} WITH (UPDLOCK, HOLDLOCK) SET {assignments}, {times} WHERE {name} = {{0}} AND ({expires} <= {now} OR {owner} = {{1}}); IF @@ROWCOUNT = 0 INSERT INTO {table} ({columns}) SELECT {values} WHERE NOT EXISTS (SELECT 1 FROM {table} WITH (UPDLOCK, HOLDLOCK) WHERE {name} = {{0}});"
            : $"INSERT INTO {table} AS {C("lease")} ({columns}) VALUES ({values}) ON CONFLICT ({name}) DO UPDATE SET {assignments}, {times} WHERE {Current("ExpiresAt")} <= {now} OR {owner} = {{1}}";
        object[] parameters = ["kernel", claim.Owner.Value, claim.ClusterId, claim.ServiceId, claim.HostName, claim.Version, Ttl(context, claim.Ttl)];
        return await context.Database.ExecuteSqlInterpolatedAsync(FormattableStringFactory.Create(sql, parameters), cancellationToken);
    }

    /// <summary>
    /// Extends only the current unexpired owner.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="ttl">The ttl.</param>
    /// <param name="cancellationToken">The cancellationToken.</param>
    /// <returns>The affected row count.</returns>
    internal static async Task<int> Renew(ClusterDbContext context, string owner, TimeSpan ttl, CancellationToken cancellationToken)
    {
        var table = await Table(context, cancellationToken);
        var helper = context.GetService<ISqlGenerationHelper>();
        string C(string name) => helper.DelimitIdentifier(name);
        var (now, expiry) = Clock(context);
        var sql = $"UPDATE {table} SET {C("RenewedAt")} = {now}, {C("ExpiresAt")} = {expiry} WHERE {C("Name")} = {{0}} AND {C("Owner")} = {{1}} AND {C("ExpiresAt")} > {now}";
        object[] parameters = ["kernel", owner, string.Empty, string.Empty, string.Empty, string.Empty, Ttl(context, ttl)];
        return await context.Database.ExecuteSqlInterpolatedAsync(FormattableStringFactory.Create(sql, parameters), cancellationToken);
    }

    /// <summary>
    /// Expires only the current owner.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="cancellationToken">The cancellationToken.</param>
    /// <returns>The completion task.</returns>
    internal static async Task Release(ClusterDbContext context, string owner, CancellationToken cancellationToken)
    {
        var table = await Table(context, cancellationToken);
        var helper = context.GetService<ISqlGenerationHelper>();
        string C(string name) => helper.DelimitIdentifier(name);
        var (now, _) = Clock(context);
        var sql = $"UPDATE {table} SET {C("ExpiresAt")} = {now} WHERE {C("Name")} = {{0}} AND {C("Owner")} = {{1}}";
        await context.Database.ExecuteSqlInterpolatedAsync(FormattableStringFactory.Create(sql, "kernel", owner), cancellationToken);
    }

    static object Ttl(ClusterDbContext context, TimeSpan ttl) => context.Database.IsSqlite()
        ? $"+{ttl.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture)} seconds"
        : checked((long)ttl.TotalMilliseconds);

    static (string Now, string Expiry) Clock(ClusterDbContext context) => context.Database.ProviderName switch
    {
        "Npgsql.EntityFrameworkCore.PostgreSQL" => ("clock_timestamp()", "clock_timestamp() + {6} * interval '1 millisecond'"),
        "Microsoft.EntityFrameworkCore.SqlServer" => ("SYSUTCDATETIME()", "DATEADD(millisecond, {6}, SYSUTCDATETIME())"),
        _ => ("strftime('%Y-%m-%d %H:%M:%f', 'now')", "strftime('%Y-%m-%d %H:%M:%f', 'now', {6})")
    };

    static async Task<string> Table(ClusterDbContext context, CancellationToken cancellationToken)
    {
        var entity = context.Model.FindEntityType(typeof(SingleWriterLeaseRow))!;
        var schema = entity.GetSchema();
        if (schema is null && !context.Database.IsSqlite())
        {
            schema = await context.Database.SqlQueryRaw<string>(
                context.Database.IsNpgsql() ? "SELECT current_schema() AS \"Value\"" : "SELECT SCHEMA_NAME() AS [Value]").SingleAsync(cancellationToken);
        }

        return context.GetService<ISqlGenerationHelper>().DelimitIdentifier(entity.GetTableName()!, schema);
    }
}
