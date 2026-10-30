// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using JobError = Cratis.Orleans.Storage.Jobs.JobError;

namespace Cratis.Chronicle.Generations.Enablement;

/// <summary>
/// Refuses unresolved legacy work; never infers abandonment or repairs it.
/// </summary>
/// <param name="storage">The durable store.</param>
public sealed class GenerationLegacyWork(IStorage storage) : IGenerationLegacyWork
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<GenerationInitializationBlocker>> Inspect(EventStoreName eventStore)
    {
        var store = storage.GetEventStore(eventStore);
        var blockers = new List<GenerationInitializationBlocker>();
        foreach (var item in await store.Namespaces.GetAll())
        {
            var ns = store.GetNamespace(item.Name);
            var contexts = await ns.ReplayContexts.GetAll();
            foreach (var context in contexts)
            {
                blockers.Add(new(eventStore, GenerationInitializationBlockerKind.LegacyReplaySessionUnresolved, $"{item.Name}/{context.Type}", $"Replay session targets {context.ContainerName}; revert {context.RevertContainerName}.", DateTimeOffset.UtcNow, null));
            }
            var replays = RequireJobs(await ns.Jobs.GetJobs<IReplayObserver, JobState>());
            var catchups = RequireJobs(await ns.Jobs.GetJobs<ICatchUpObserver, JobState>());
            var unfinished = replays.Concat(catchups).Where(job => job.Status is not JobStatus.CompletedSuccessfully and not JobStatus.CompletedWithFailures).ToArray();
            foreach (var job in unfinished)
            {
                blockers.Add(new(eventStore, Enum.IsDefined(job.Status) ? GenerationInitializationBlockerKind.LegacyReplayJobUnresolved : GenerationInitializationBlockerKind.UnknownLegacyState, $"{item.Name}/{job.Id}", $"Legacy job {job.Type} is {job.Status}.", DateTimeOffset.UtcNow, null));
            }
            if (contexts.Count == 0 && unfinished.Length == 0)
            {
                foreach (var observer in (await ns.Observers.GetAll()).Where(observer => observer.IsReplaying || observer.ReplayingPartitions.Count != 0 || observer.CatchingUpPartitions.Count != 0))
                {
                    blockers.Add(new(eventStore, GenerationInitializationBlockerKind.ContradictoryLegacyState, $"legacy-work/{item.Name}/{observer.Identifier}", "Observer replay or catch-up state has no owning legacy work.", DateTimeOffset.UtcNow, null));
                }
            }
        }
        return blockers;
    }

    static IEnumerable<JobState> RequireJobs(Cratis.Monads.Catch<IImmutableList<JobState>, JobError> result) => result.Match<IEnumerable<JobState>>(
        jobs => jobs,
        error => throw new InvalidOperationException($"Legacy job inventory cannot be classified ({error})."),
        exception => throw exception);
}
