// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Replaying;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Recommendations;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// The retirement part of <see cref="ProjectionsManager"/>: when a full-set registration no longer contains a
/// registered projection, the projection is retired - its observer stops consuming events, its jobs and failed
/// partitions are cleared, and its definition is removed everywhere it lives. Its sink container is deliberately
/// left untouched: data is never deleted implicitly. When another registered projection targets the same container,
/// a replay recommendation is raised for it so the container can be rebuilt cleanly.
/// </summary>
public partial class ProjectionsManager
{
    async Task RetireUnregisteredProjections(IReadOnlyList<ProjectionDefinition> registeredDefinitions, ProjectionOwner owner)
    {
        var registeredIdentifiers = registeredDefinitions.Select(definition => definition.Identifier).ToHashSet();
        var orphans = State.Projections
            .Where(projection => projection.Owner == owner && !registeredIdentifiers.Contains(projection.Identifier))
            .ToList();
        if (orphans.Count == 0)
        {
            return;
        }

        var namespaces = (await GrainFactory.GetGrain<INamespaces>(_eventStoreName).GetAll()).ToList();
        var readModelDefinitions = await GrainFactory.GetGrain<IReadModelsManager>(_eventStoreName).GetDefinitions();

        foreach (var orphan in orphans)
        {
            try
            {
                logger.RetiringProjection(orphan.Identifier);

                foreach (var @namespace in namespaces)
                {
                    await StopObserverFor(orphan, @namespace);
                }

                await projectionsService.Unregister(_eventStoreName, orphan.Identifier);
                await GrainFactory.GetGrain<IProjection>(new ProjectionKey(orphan.Identifier, _eventStoreName)).Remove();

                State.Projections = State.Projections.Where(projection => projection.Identifier != orphan.Identifier).ToList();

                await AddReplayRecommendationForContainerSuccessors(orphan, namespaces, readModelDefinitions);
            }
            catch (Exception exception)
            {
                // The projection stays in the registered state so the next full-set registration retries
                // retiring it, rather than leaving it half retired and forgotten.
                logger.FailedRetiringProjection(exception, orphan.Identifier);
            }
        }

        await WriteStateAsync();
    }

    async Task StopObserverFor(ProjectionDefinition orphan, EventStoreNamespaceName @namespace)
    {
        var observer = GrainFactory.GetGrain<IObserver>(new ObserverKey(orphan.Identifier, _eventStoreName, @namespace, orphan.EventSequenceId));
        await observer.Unsubscribe();

        var jobsManager = GrainFactory.GetJobsManager(_eventStoreName, @namespace);
        var jobs = await jobsManager.GetAllJobs();
        var observerJobs = jobs
            .Where(job => job.Request is IObserverJobRequest observerJobRequest &&
                observerJobRequest.ObserverKey.ObserverId == (ObserverId)orphan.Identifier.Value)
            .ToList();
        foreach (var job in observerJobs)
        {
            await jobsManager.Delete(job.Id);
        }

        await storage.GetEventStore(_eventStoreName).GetNamespace(@namespace).FailedPartitions.Save(orphan.Identifier.Value, new FailedPartitions());
    }

    async Task AddReplayRecommendationForContainerSuccessors(
        ProjectionDefinition orphan,
        IEnumerable<EventStoreNamespaceName> namespaces,
        IEnumerable<ReadModelDefinition> readModelDefinitions)
    {
        var orphanReadModel = readModelDefinitions.SingleOrDefault(readModel => readModel.Identifier == orphan.ReadModel);
        if (orphanReadModel is null)
        {
            return;
        }

        var successors = State.Projections
            .Select(projection => (Projection: projection, ReadModel: readModelDefinitions.SingleOrDefault(readModel => readModel.Identifier == projection.ReadModel)))
            .Where(candidate => candidate.ReadModel is not null && candidate.ReadModel.ContainerName == orphanReadModel.ContainerName)
            .ToList();

        foreach (var (successor, readModel) in successors)
        {
            logger.RetiredProjectionSharedContainer(orphan.Identifier, readModel!.ContainerName, successor.Identifier);
            foreach (var @namespace in namespaces)
            {
                var recommendationsManager = GrainFactory.GetGrain<IRecommendationsManager>(0, new RecommendationsManagerKey(_eventStoreName, @namespace));
                await recommendationsManager.Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(
                    $"Projection '{orphan.Identifier}' was retired and wrote to the same container '{readModel.ContainerName}' as projection '{successor.Identifier}'. Replay the projection to rebuild the container from its definition alone.",
                    new()
                    {
                        ObserverId = successor.Identifier.Value,
                        ObserverKey = new(successor.Identifier.Value, _eventStoreName, @namespace, successor.EventSequenceId),
                        Reasons = [new RetiredProjectionSharedContainerReplayCandidateReason()]
                    });
            }
        }
    }
}
