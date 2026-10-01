// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.Reducers;

/// <summary>
/// Serializes admission and publication, while replay workers write only to attempt-specific targets.
/// </summary>
/// <param name="storage">The storage.</param>
/// <param name="options">The replay retention policy.</param>
/// <param name="logger">The logger.</param>
public class ReducerReplay(IStorage storage, IOptions<Configuration.ChronicleOptions> options, ILogger<ReducerReplay> logger) : Grain, IReducerReplay
{
    /// <inheritdoc/>
    public async Task<ReplayContext> Begin(JobId jobId)
    {
        var key = ObserverKey.Parse(this.GetPrimaryKeyString());
        var namespaceStorage = storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace);
        var readModel = await GetReadModel(key);
        var current = await namespaceStorage.ReplayContexts.TryGet(readModel.Identifier);
        if (current.TryGetResult(out var previous) && TryGetJobId(previous, out var previousJobId))
        {
            // GetJob returns only base JobState on SQL. Read the concrete state or the durable
            // Publishing intent disappears during recovery even though its JSON is still present.
            var previousJob = await namespaceStorage.Jobs.Read<JobStateWithLastHandledEvent>(previousJobId);
            if (previousJob.TryGetException(out var readFailure)) throw readFailure;
            if (previousJob.TryGetError(out var readError) && readError != Cratis.Orleans.Storage.Jobs.JobError.NotFound)
            {
                throw new ReplayFinalizationFailed(ICanHandleReplayForObserver.Error.Unknown);
            }

            if (previousJob.TryGetResult(out var replay) && replay.ReducerReplayPhase == ReducerReplayPhase.Publishing)
            {
                // Publication may have committed before its RPC reply was lost. Reconcile it before any
                // new attempt can create a primary collection or reuse that name, including index creation.
                _ = await Publish(Isolated(previous) with { AllowEmptyResult = replay.LastHandledEventSequenceNumber.IsActualValue });
            }
        }

        var context = Isolated(new ReplayContext(
            new(readModel.Identifier, readModel.LatestGeneration),
            readModel.ContainerName,
            $"rr-{Guid.NewGuid().ToString("N")[..16]}-{jobId.Value:N}",
            DateTimeOffset.UtcNow));
        var sink = await namespaceStorage.Sinks.GetFor(readModel, ensureIndexes: false);
        _ = await namespaceStorage.Sinks.GetFor(readModel with { ContainerName = context.ReplayContainerName! }, ensureIndexes: false);
        await sink.PrepareReplay(context);
        await namespaceStorage.ReplayContexts.Save(context);
        var manager = GrainFactory.GetReadModelReplayManager(key.EventStore, key.Namespace, readModel.Identifier);
        await manager.ApplyRetentionPolicy(options.Value.ReadModels.ReplayedVersionsToKeep);
        return context;
    }

    /// <inheritdoc/>
    public async Task<ReplayPublication> Publish(ReplayContext context)
    {
        var key = ObserverKey.Parse(this.GetPrimaryKeyString());
        var namespaceStorage = storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace);
        var current = await namespaceStorage.ReplayContexts.TryGet(context.Type.Identifier);
        if (!current.TryGetResult(out var active) || active.RevertContainerName != context.RevertContainerName)
        {
            return ReplayPublication.Superseded;
        }

        var readModel = await GetReadModel(key);

        // Resolving a sink for finalization must not create indexes/collections. A restart between renames
        // can legitimately find the primary absent; recreating it would disguise an incomplete swap.
        var sink = await namespaceStorage.Sinks.GetFor(readModel, ensureIndexes: false);
        var replaySink = await namespaceStorage.Sinks.GetFor(readModel with { ContainerName = Isolated(context).ReplayContainerName! }, ensureIndexes: false);
        var failures = (await sink.PublishReplay(Isolated(context), replaySink)).ToArray();
        if (failures.Length > 0)
        {
            throw new ReplayFinalizationFailed(ICanHandleReplayForObserver.Error.Unknown);
        }

        // The swap is the commit point. Nothing after it is allowed to turn Published into Abandoned.
        // Keep the identity in existing replay-context storage so a lost reply can retry the idempotent swap.
        try
        {
            var manager = GrainFactory.GetReadModelReplayManager(key.EventStore, key.Namespace, readModel.Identifier);
            await manager.Replayed(key.ObserverId, context);
            return ReplayPublication.Published;
        }
        catch (Exception exception)
        {
            logger.BookkeepingFailed(exception, key.ObserverId);
            return ReplayPublication.PublishedWithBookkeepingFailure;
        }
    }

    /// <inheritdoc/>
    public async Task Abandon(JobId jobId)
    {
        var key = ObserverKey.Parse(this.GetPrimaryKeyString());
        var namespaceStorage = storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace);
        var readModel = await GetReadModel(key);
        var current = await namespaceStorage.ReplayContexts.TryGet(readModel.Identifier);
        if (current.TryGetResult(out var context) && TryGetJobId(context, out var owner) && owner == jobId)
        {
            // Keep the abandoned target as recovery evidence. Late client replies can only write there,
            // never into the live model or the next replay. Removing a job is not proof those replies drained.
            await namespaceStorage.ReplayContexts.Evict(readModel.Identifier);
        }
    }

    static ReplayContext Isolated(ReplayContext context) => context with { ReplayContainerName = $"replay-{context.RevertContainerName}" };

    static bool TryGetJobId(ReplayContext context, out JobId jobId)
    {
        var name = context.RevertContainerName.Value;
        var parsed = name.Length >= 32 && Guid.TryParseExact(name[^32..], "N", out _);
        jobId = parsed ? Guid.ParseExact(name[^32..], "N") : JobId.NotSet;
        return parsed;
    }

    async Task<ReadModelDefinition> GetReadModel(ObserverKey key)
    {
        var eventStore = storage.GetEventStore(key.EventStore);
        var definition = await eventStore.Reducers.Get(new ReducerId(key.ObserverId.Value));
        return await eventStore.ReadModels.Get(definition.ReadModel);
    }
}
