// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Represents a scoped open-incident lookup.
/// </summary>
/// <param name="Status">Sampled reactor health.</param>
/// <param name="Incident">Matching open incident, or normal not-found.</param>
[ReadModel]
[BelongsTo(WellKnownServices.Alerts)]
public record AlertIncidentLookup(
    AlertIncidentsReadinessState Status,
    AlertIncidentDetails? Incident)
{
    /// <summary>
    /// Looks up an open incident within an affected store.
    /// </summary>
    /// <param name="eventStore">Required affected store.</param>
    /// <param name="incidentId">Incident identity.</param>
    /// <param name="storage">Storage registry.</param>
    /// <param name="readiness">Sampled materialization health.</param>
    /// <param name="namespace">Optional affected namespace.</param>
    /// <returns>The scoped lookup with its sampled health.</returns>
    internal static async Task<AlertIncidentLookup> GetOpenIncident(
        EventStoreName eventStore,
        IncidentId incidentId,
        IStorage storage,
        IAlertIncidentsReadiness readiness,
        EventStoreNamespaceName? @namespace = null)
    {
        var scope = AlertIncidentQueryArguments.Scope(eventStore, @namespace);
        var status = await readiness.Get();
        var incident = await storage.GetEventStore(EventStoreName.System).GetNamespace(EventStoreNamespaceName.Default).AlertIncidents
            .GetOpen(scope, incidentId);

        return new(status, incident?.ToDetails());
    }
}
