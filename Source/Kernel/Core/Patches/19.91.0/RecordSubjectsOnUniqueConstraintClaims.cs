// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Patching;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Patches;

/// <summary>
/// Patch to rebuild the unique constraint indexes that cover personal data, so every claim derived from it records its subject.
/// </summary>
/// <remarks>
/// Claims written before unique-index entries carried their subject hold none, so erasing a subject cannot find them.
/// Rebuilding each unique constraint that covers a personal-data property, in any generation of its event types, from the event log re-derives each claim with its subject, and skips the values
/// of subjects that are already erased, so those values become claimable again. While a constraint rebuilds, governed
/// appends to it are refused.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for accessing storage.</param>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
public class RecordSubjectsOnUniqueConstraintClaims(IStorage storage, IGrainFactory grainFactory, ILogger<RecordSubjectsOnUniqueConstraintClaims> logger) : ICanApplyPatch
{
    /// <inheritdoc/>
    public SemanticVersion Version => new(19, 91, 0);

    /// <inheritdoc/>
    public async Task Up()
    {
        logger.StartingPatch();

        var eventStores = await storage.GetEventStores();
        foreach (var eventStore in eventStores)
        {
            var eventStoreStorage = storage.GetEventStore(eventStore);
            var definitions = await eventStoreStorage.Constraints.GetDefinitions();
            var uniqueConstraintDefinitions = await PersonalDataConstraints(eventStoreStorage.EventTypes, definitions.OfType<UniqueConstraintDefinition>());

            if (uniqueConstraintDefinitions.Length == 0)
            {
                continue;
            }

            logger.FoundUniqueConstraints(eventStore, uniqueConstraintDefinitions.Length);

            var changes = uniqueConstraintDefinitions
                .Select(_ => new ConstraintDefinitionChange(_.Name, true, [ConstraintChangeType.IndexedPropertiesChanged]))
                .ToArray();

            var namespaces = grainFactory.GetGrain<INamespaces>(eventStore);
            var namespaceNames = await namespaces.GetAll();

            foreach (var namespaceName in namespaceNames)
            {
                logger.RebuildingConstraintIndexes(eventStore, namespaceName);

                var jobsManager = grainFactory.GetJobsManager(eventStore, namespaceName);
                var result = await jobsManager.Start<IReindexConstraints, ReindexConstraintsRequest>(new(EventSequenceId.Log, changes));
                if (result.TryGetError(out var error))
                {
                    logger.FailedRebuildingConstraintIndexes(eventStore, namespaceName, error.ToString());
                }
            }
        }

        logger.PatchCompleted();
    }

    /// <inheritdoc/>
    public Task Down()
    {
        logger.RollbackNotSupported();
        return Task.CompletedTask;
    }

    static async Task<UniqueConstraintDefinition[]> PersonalDataConstraints(IEventTypesStorage eventTypes, IEnumerable<UniqueConstraintDefinition> definitions)
    {
        var personalProperties = new Dictionary<EventTypeId, HashSet<string>>();
        var result = new List<UniqueConstraintDefinition>();
        foreach (var definition in definitions)
        {
            foreach (var eventDefinition in definition.EventDefinitions)
            {
                if (!personalProperties.TryGetValue(eventDefinition.EventTypeId, out var properties))
                {
                    var generations = await eventTypes.GetAllGenerationsForEventType(new EventType(eventDefinition.EventTypeId, EventTypeGeneration.First));
                    properties = new HashSet<string>(generations.SelectMany(_ => ConstraintPersonalData.PropertiesOf(_.Schema)), StringComparer.OrdinalIgnoreCase);
                    personalProperties[eventDefinition.EventTypeId] = properties;
                }

                if (eventDefinition.Properties.Any(property => properties.Contains(property.Split('.')[0])))
                {
                    result.Add(definition);
                    break;
                }
            }
        }

        return [.. result];
    }
}
