// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*
Conditional statement shapes derived from dotnet/orleans AdoNet clustering (MIT).
Copyright (c) .NET Foundation and Contributors. All rights reserved.
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Executes parameterized conditional membership statements.
/// </summary>
internal static class MembershipSql
{
    /// <summary>
    /// Creates the initial version only when absent.
    /// </summary>
    /// <param name="context">The cluster context.</param>
    /// <param name="clusterId">The cluster identifier.</param>
    /// <returns>The number of inserted versions.</returns>
    internal static async Task<int> Initialize(ClusterDbContext context, string clusterId)
    {
        var table = await Table<MembershipVersion>(context);
        var column = context.GetService<ISqlGenerationHelper>();
        var cluster = column.DelimitIdentifier(nameof(MembershipVersion.ClusterId));
        var version = column.DelimitIdentifier(nameof(MembershipVersion.Version));
        var timestamp = column.DelimitIdentifier(nameof(MembershipVersion.Timestamp));
        var sql = context.Database.IsNpgsql()
            ? $"INSERT INTO {table} ({cluster}, {version}, {timestamp}) VALUES ({{0}}, 0, {{1}}) ON CONFLICT ({cluster}) DO NOTHING"
            : $"INSERT INTO {table} ({cluster}, {version}, {timestamp}) SELECT {{0}}, 0, {{1}} WHERE NOT EXISTS (SELECT 1 FROM {table} WITH (UPDLOCK, HOLDLOCK) WHERE {cluster} = {{0}})";
        return await context.Database.ExecuteSqlInterpolatedAsync(FormattableStringFactory.Create(sql, clusterId, DateTime.UtcNow));
    }

    /// <summary>
    /// Inserts the silo only when its composite key is absent.
    /// </summary>
    /// <param name="context">The cluster context.</param>
    /// <param name="row">The membership row.</param>
    /// <returns>The number of inserted rows.</returns>
    internal static async Task<int> Insert(ClusterDbContext context, MembershipRow row)
    {
        var table = await Table<MembershipRow>(context);
        var helper = context.GetService<ISqlGenerationHelper>();
        string[] names = [nameof(row.ClusterId), nameof(row.Address), nameof(row.Port), nameof(row.Generation),
            nameof(row.SiloName), nameof(row.HostName), nameof(row.Status), nameof(row.ProxyPort), nameof(row.SuspectTimes),
            nameof(row.StartTime), nameof(row.IAmAliveTime), nameof(row.ETag), nameof(row.RoleName), nameof(row.UpdateZone), nameof(row.FaultZone)];
        object[] values = [row.ClusterId, row.Address, row.Port, row.Generation, row.SiloName, row.HostName,
            row.Status, row.ProxyPort, row.SuspectTimes!, row.StartTime, row.IAmAliveTime, row.ETag, row.RoleName!, row.UpdateZone, row.FaultZone];
        var columns = string.Join(", ", names.Select(helper.DelimitIdentifier));
        var parameters = string.Join(", ", names.Select((_, index) => $"{{{index}}}"));
        var predicate = string.Join(" AND ", names.Take(4).Select((name, index) => $"{helper.DelimitIdentifier(name)} = {{{index}}}"));
        var sql = context.Database.IsNpgsql()
            ? $"INSERT INTO {table} ({columns}) VALUES ({parameters}) ON CONFLICT DO NOTHING"
            : $"INSERT INTO {table} ({columns}) SELECT {parameters} WHERE NOT EXISTS (SELECT 1 FROM {table} WHERE {predicate})";
        return await context.Database.ExecuteSqlInterpolatedAsync(FormattableStringFactory.Create(sql, values));
    }

    static async Task<string> Table<TEntity>(ClusterDbContext context)
    {
        var entity = context.Model.FindEntityType(typeof(TEntity))!;
        var schema = entity.GetSchema() ?? await context.Database.SqlQueryRaw<string>(
            context.Database.IsNpgsql() ? "SELECT current_schema() AS \"Value\"" : "SELECT SCHEMA_NAME() AS [Value]").SingleAsync();
        return context.GetService<ISqlGenerationHelper>().DelimitIdentifier(entity.GetTableName()!, schema);
    }
}
