// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.for_EventStoreNamespaceStorage;

public class when_only_operational_collections_exist : given.a_namespace_database
{
    bool _hasEvents;
    bool _hasState;

    void Establish()
    {
        _names.AddRange([
            WellKnownCollectionNames.EventSequences,
            WellKnownCollectionNames.EventSequenceMutationHeads,
            WellKnownCollectionNames.EventSequenceMutationHistory,
            WellKnownCollectionNames.Observers,
            WellKnownCollectionNames.ObserverDefinitionChangeProgress,
            WellKnownCollectionNames.ObserverDefinitionChangeContinuations,
            WellKnownCollectionNames.ObserverDefinitionChangeRestartPlans,
            WellKnownCollectionNames.ObserverDefinitionChangeInventory,
            WellKnownCollectionNames.AlertIncidents,
            WellKnownCollectionNames.FailedPartitions,
            WellKnownCollectionNames.InFlightEvents,
            WellKnownCollectionNames.ObserverHandledCounts,
            WellKnownCollectionNames.Identities,
            WellKnownCollectionNames.EncryptionKeys,
            WellKnownCollectionNames.EncryptionKeyErasures,
            WellKnownCollectionNames.Jobs,
            WellKnownCollectionNames.JobSteps,
            WellKnownCollectionNames.FailedJobSteps,
            WellKnownCollectionNames.JobStepPlans,
            WellKnownCollectionNames.Recommendations,
            WellKnownCollectionNames.BehaviorPatterns,
            WellKnownCollectionNames.ReplayContexts,
            WellKnownCollectionNames.ReplayedReadModels,
            WellKnownCollectionNames.EventSeeds,
            WellKnownCollectionNames.Imports,
            WellKnownCollectionNames.ImportChunks,
            WellKnownCollectionNames.ProjectionFutures,
            WellKnownCollectionNames.Occurrences,
            WellKnownCollectionNames.OccurrenceClaimFences,
            WellKnownCollectionNames.TriggerDefinitions,
            WellKnownCollectionNames.LifecycleFanOutIntents,
            WellKnownCollectionNames.ScheduleDefinitions,
            WellKnownCollectionNames.ScheduleCursors,
            WellKnownCollectionNames.SkippedRanges,
            WellKnownCollectionNames.EffectReceipts,
            WellKnownCollectionNames.CapabilityProbe,
            WellKnownCollectionNames.CapabilityProbePrefix + "one",
            "system.views"]);
        _nonEmpty.UnionWith(_names);
        _names.AddRange([WellKnownCollectionNames.EventLog, "custom-events"]);
    }

    async Task Because()
    {
        _hasEvents = await _database.HasEvents();
        _hasState = await _database.HasAnyCollections();
    }

    [Fact] void should_not_report_event_data() => _hasEvents.ShouldBeFalse();
    [Fact] void should_preserve_the_conservative_state_check() => _hasState.ShouldBeTrue();
    [Fact] void should_only_read_possible_event_collections() => _probes.ShouldContainOnly(WellKnownCollectionNames.EventLog, "custom-events");
}
