// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Alerts;

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Represents a scoped page and its sampled materialization health.
/// </summary>
/// <param name="Status">Sampled reactor health.</param>
/// <param name="Items">Matching open incidents.</param>
/// <param name="Next">Continuation if more rows exist.</param>
[ReadModel]
[BelongsTo(WellKnownServices.Alerts)]
public record AlertIncidentPage(
    AlertIncidentsReadinessState Status,
    IEnumerable<AlertIncidentDetails> Items,
    AlertIncidentContinuation? Next)
{
    /// <summary>
    /// Pages recorded open incidents within an affected store.
    /// </summary>
    /// <param name="eventStore">Required affected store.</param>
    /// <param name="storage">Storage registry.</param>
    /// <param name="readiness">Sampled materialization health.</param>
    /// <param name="namespace">Optional affected namespace.</param>
    /// <param name="observerId">Optional observer filter.</param>
    /// <param name="condition">Optional condition filter.</param>
    /// <param name="minimumSeverity">Optional minimum severity.</param>
    /// <param name="limit">Requested limit, bounded to 1–500.</param>
    /// <param name="afterRaisedSequenceNumber">Exclusive raise position paired with identity.</param>
    /// <param name="afterIncidentId">Exclusive identity paired with raise position.</param>
    /// <returns>The page with its sampled health.</returns>
    internal static async Task<AlertIncidentPage> GetOpenIncidents(
        EventStoreName eventStore,
        IStorage storage,
        IAlertIncidentsReadiness readiness,
        EventStoreNamespaceName? @namespace = null,
        ObserverId? observerId = null,
        AlertConditionKind? condition = null,
        AlertSeverity? minimumSeverity = null,
        int limit = 100,
        EventSequenceNumber? afterRaisedSequenceNumber = null,
        IncidentId? afterIncidentId = null)
    {
        var scope = AlertIncidentQueryArguments.Scope(eventStore, @namespace);
        var after = AlertIncidentQueryArguments.Cursor(afterRaisedSequenceNumber, afterIncidentId);
        var page = await storage.GetEventStore(EventStoreName.System).GetNamespace(EventStoreNamespaceName.Default).AlertIncidents
            .GetOpenPage(new(scope, observerId, condition, minimumSeverity), after, AlertIncidentStorageRules.Limit(limit));
        var status = await readiness.Get();

        return new(status, page.Items.Select(row => row.ToDetails()).ToArray(), page.Next?.ToContinuation());
    }
}
