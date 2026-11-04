// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Diagnostics;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Traces;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// Starts unit of work spans with the selected naming convention.
/// </summary>
internal static class UnitOfWorkTraces
{
    static readonly ClientSpanDefinition _commit = new("client.unit_of_work.commit", WellKnownClientSpanNames.UnitOfWorkCommit, ActivityKind.Client)
    {
        LegacyKeys = ["correlation_id"],
        ConventionKeys = [WellKnownTelemetryAttributes.CorrelationId]
    };

    static readonly ClientSpanDefinition _rollback = new("client.unit_of_work.rollback", WellKnownClientSpanNames.UnitOfWorkRollback, ActivityKind.Client)
    {
        LegacyKeys = ["correlation_id"],
        ConventionKeys = [WellKnownTelemetryAttributes.CorrelationId]
    };

    /// <summary>Starts a commit span.</summary>
    /// <param name="source">The activity source.</param>
    /// <param name="correlationId">The correlation identifier.</param>
    /// <param name="names">The naming convention.</param>
    /// <returns>The activity scope.</returns>
    internal static IActivityScope<UnitOfWork> Commit(this IActivitySource<UnitOfWork> source, string correlationId, ClientSpanNames names = ClientSpanNames.Legacy) =>
        ClientSpanCompatibility.Start(source, _commit, names, new(correlationId));

    /// <summary>Starts a rollback span.</summary>
    /// <param name="source">The activity source.</param>
    /// <param name="correlationId">The correlation identifier.</param>
    /// <param name="names">The naming convention.</param>
    /// <returns>The activity scope.</returns>
    internal static IActivityScope<UnitOfWork> Rollback(this IActivitySource<UnitOfWork> source, string correlationId, ClientSpanNames names = ClientSpanNames.Legacy) =>
        ClientSpanCompatibility.Start(source, _rollback, names, new(correlationId));
}
