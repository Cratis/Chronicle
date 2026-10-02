// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Represents complete scoped counts and sampled materialization health.
/// </summary>
/// <param name="Status">Sampled reactor health.</param>
/// <param name="Counts">Complete scoped counts.</param>
[ReadModel]
[BelongsTo(WellKnownServices.Alerts)]
public record AlertIncidentSummary(
    AlertIncidentsReadinessState Status,
    IEnumerable<AlertIncidentCountDetails> Counts)
{
    /// <summary>
    /// Counts the complete open population within an affected store.
    /// </summary>
    /// <param name="eventStore">Required affected store.</param>
    /// <param name="storage">Storage registry.</param>
    /// <param name="readiness">Sampled materialization health.</param>
    /// <param name="namespace">Optional affected namespace.</param>
    /// <returns>The complete counts with sampled health.</returns>
    internal static async Task<AlertIncidentSummary> GetOpenIncidentCounts(
        EventStoreName eventStore,
        IStorage storage,
        IAlertIncidentsReadiness readiness,
        EventStoreNamespaceName? @namespace = null)
    {
        var scope = AlertIncidentQueryArguments.Scope(eventStore, @namespace);
        var counts = await storage.GetEventStore(EventStoreName.System).GetNamespace(EventStoreNamespaceName.Default).AlertIncidents.GetOpenCounts(scope);
        var status = await readiness.Get();

        return new(status, counts.Select(count => count.ToDetails()).ToArray());
    }
}
