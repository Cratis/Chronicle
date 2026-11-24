// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine.Expressions.Keys;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Projections;

public partial class ProjectionsManager
{
    async Task<List<ProjectionDefinition>> PreflightFlatKeys(IEnumerable<ProjectionDefinition> definitions, Dictionary<ProjectionId, Exception> failures)
    {
        var accepted = new List<ProjectionDefinition>();
        foreach (var definition in definitions)
        {
            try
            {
                RefuseKeyModeChange(definition);
                if (FlatCompositeKeyDefinition.HasFlatKey(definition))
                {
                    await ValidateFlatKeySink(definition);
                }

                accepted.Add(definition);
            }
            catch (Exception exception)
            {
                failures[definition.Identifier] = exception;
            }
        }

        return accepted;
    }

    ProjectionDefinition[] PreserveRejectedMaterializations(IEnumerable<ProjectionDefinition> submitted, HashSet<ProjectionId> rejected)
    {
        var definitions = submitted.ToArray();
        var readModels = definitions.Where(definition => rejected.Contains(definition.Identifier)).Select(definition => definition.ReadModel).ToHashSet();
        return definitions.Concat(State.Projections.Where(stored => readModels.Contains(stored.ReadModel))).DistinctBy(definition => definition.Identifier).ToArray();
    }

    void RefuseKeyModeChange(ProjectionDefinition definition)
    {
        var isFlat = FlatCompositeKeyDefinition.UsesFlatKey(definition);
        var materializations = State.Projections.Where(stored => stored.Identifier == definition.Identifier || stored.ReadModel == definition.ReadModel);
        if (materializations.Any(stored => FlatCompositeKeyDefinition.UsesFlatKey(stored) != isFlat))
        {
            throw new FlatCompositeKeyNotSupported("Changing the key mode of an existing projection or read model is not supported. Use a new read model for flat composite keys.");
        }
    }

    async Task ValidateFlatKeySink(ProjectionDefinition definition)
    {
        var readModels = await GrainFactory.GetGrain<IReadModelsManager>(_eventStoreName).GetDefinitions();
        var readModel = readModels.FirstOrDefault(model => model.Identifier == definition.ReadModel)
            ?? throw new ReadModelNotFound(definition.ReadModel);
        var sinkNamespace = definition.Scope == ProjectionScope.Global ? EventStoreNamespaceName.NotSet : EventStoreNamespaceName.Default;
        var sinks = storage.GetEventStore(_eventStoreName).GetNamespace(sinkNamespace).Sinks;
        if (!sinks.SupportsFlatCompositeKeys(readModel))
        {
            throw new FlatCompositeKeyNotSupported("The configured sink does not support flat composite keys.");
        }

        FlatCompositeKeyDefinition.Validate(definition, readModel);
    }
}
