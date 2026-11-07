// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Connections;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// The part of <see cref="Database"/> that reports encryption key identifiers awaiting review.
/// </summary>
public partial class Database
{
    /// <summary>
    /// Logs how many encryption key identifiers await review, once per namespace database and process. Only the count is logged.
    /// </summary>
    /// <param name="context">The migrated <see cref="DbContext"/>.</param>
    /// <param name="target">The <see cref="SqlTarget"/> the context connects to.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    internal async Task ReportIdentifiersAwaitingReview(DbContext context, SqlTarget target)
    {
        if (context is not NamespaceDbContext || !context.Database.IsSqlServer() || !_reviewsReported.TryAdd(target.ConnectionString, true))
        {
            return;
        }

        var count = await context.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM [EncryptionKeyIdentifierReviews]").SingleAsync();
        if (count > 0)
        {
            DatabaseLogging.IdentifiersAwaitingReview(_encryptionKeyLogger.Value, count);
        }
    }
}
