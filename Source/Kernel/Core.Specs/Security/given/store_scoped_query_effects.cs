// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static Cratis.Chronicle.Security.given.store_effect;
using static Cratis.Chronicle.Security.given.store_scoped_surface_classification;

namespace Cratis.Chronicle.Security.given;

public static class store_scoped_query_effects
{
    /// <summary>
    /// Overload-aware identities remain the dictionary keys. These effects apply to every listed overload.
    /// </summary>
    /// <param name="id">The overload-aware query identity.</param>
    /// <returns>The access rights and effects of the query.</returns>
    public static store_scoped_surface Classify(string id) => id[..id.IndexOf('(')] switch
    {
        "Cratis.Chronicle.Captures.CaptureDetails.ObserveCaptures" or
        "Cratis.Chronicle.EventSources.EventSourceDetails.ObserveEventSources" or
        "Cratis.Chronicle.EventTypes.EventTypeDetails.ObserveEventTypes" or
        "Cratis.Chronicle.ExternalServices.ExternalService.ObserveExternalServices" or
        "Cratis.Chronicle.Jobs.JobSummary.ObserveJobs" or
        "Cratis.Chronicle.Namespaces.NamespaceNames.ObserveNamespaces" or
        "Cratis.Chronicle.Observation.ObserverInformation.ObserveObservers" or
        "Cratis.Chronicle.Observation.ObserverInformationForEventType.ObserveObserversForEventType" or
        "Cratis.Chronicle.Observation.Webhooks.WebhookDetails.ObserveWebhooks" or
        "Cratis.Chronicle.Occurrences.OccurrenceSnapshot.ObserveOccurrences" => Read(id, store_effect.Stream, Enumerates),
        "Cratis.Chronicle.Sequences.ExportedEvent.ExportEvents" => Read(id, Exports, Enumerates),
        "Cratis.Chronicle.Alerts.AlertIncidentLookup.GetOpenIncident" or
        "Cratis.Chronicle.Alerts.AlertIncidentSummary.GetOpenIncidentCounts" or
        "Cratis.Chronicle.EventStores.EventStoreDeployment.GetEventStoreDeployment" or
        "Cratis.Chronicle.Occurrences.EffectReceiptRecord.GetEffectReceipt" or
        "Cratis.Chronicle.Occurrences.OccurrenceSnapshot.GetOccurrence" or
        "Cratis.Chronicle.ReadModelLookups.ReadModelLookup.FindByCompositeKey" or
        "Cratis.Chronicle.Sequences.EventSequenceTail.TailSequenceNumber" or
        "Cratis.Chronicle.Sequences.EventSourceEvents.HasEventsForEventSourceId" or
        "Cratis.Chronicle.Sequences.StreamScopeCompletion.IsStreamScopeCompleted" or
        "Cratis.Chronicle.Statistics.EventStatistics.StatisticsForEventStore" or
        "Cratis.Chronicle.Statistics.EventStatistics.StatisticsForNamespace" => Read(id),
        _ => Read(id, Enumerates)
    };
}
