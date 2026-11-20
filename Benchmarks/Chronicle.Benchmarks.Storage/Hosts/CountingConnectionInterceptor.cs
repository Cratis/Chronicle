// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Benchmarks.Storage.Hosts;

/// <summary>
/// Replaces the connection EF Core creates with a <see cref="CountingDbConnection"/>, so the counter sees every
/// command at the ADO.NET level, including those issued directly on the connection (for example by the table migrator).
/// </summary>
/// <param name="counter">The counter to record into.</param>
internal sealed class CountingConnectionInterceptor(CommandCounter counter) : DbConnectionInterceptor
{
    /// <inheritdoc/>
    public override DbConnection ConnectionCreated(ConnectionCreatedEventData eventData, DbConnection result) =>
        result is CountingDbConnection ? result : new CountingDbConnection(result, counter);
}
