// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Alerts;
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
    /// <param name="limit">Requested limit; zero selects the default 100, positive values clamp to 500, and negatives are rejected.</param>
    /// <param name="afterRaisedSequenceNumber">Exclusive raise position, defaulting to zero and read only when an identity is supplied.</param>
    /// <param name="afterIncidentId">Exclusive identity; a non-empty identity enables continuation.</param>
    /// <returns>The page with its sampled health.</returns>
    /// <exception cref="InvalidAlertIncidentQuery">The scope, cursor, or page limit is invalid.</exception>
    internal static async Task<AlertIncidentPage> GetOpenIncidents(
        EventStoreName eventStore,
        IStorage storage,
        IAlertIncidentsReadiness readiness,
        EventStoreNamespaceName? @namespace = null,
        ObserverId? observerId = null,
        AlertConditionKind? condition = null,
        AlertSeverity? minimumSeverity = null,
        int limit = 0,
        ulong afterRaisedSequenceNumber = 0,
        IncidentId? afterIncidentId = null)
    {
        var scope = AlertIncidentQueryArguments.Scope(eventStore, @namespace);
        var after = AlertIncidentQueryArguments.Cursor(afterRaisedSequenceNumber, afterIncidentId);
        if (limit < 0)
        {
            throw new InvalidAlertIncidentQuery("The page limit cannot be negative.");
        }
        var pageLimit = AlertIncidentStorageRules.Limit(limit == 0 ? 100 : limit);
        var status = await readiness.Get();
        var page = await storage.GetEventStore(EventStoreName.System).GetNamespace(EventStoreNamespaceName.Default).AlertIncidents
            .GetOpenPage(new(scope, observerId, condition, minimumSeverity), after, pageLimit);

        return new(status, page.Items.Select(row => row.ToDetails()).ToArray(), page.Next?.ToContinuation());
    }
}
