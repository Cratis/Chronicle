// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;
using EngineProjection = Cratis.Chronicle.Projections.Engine.IProjection;

namespace Cratis.Chronicle.Projections.Engine.Pipelines;

/// <summary>
/// Represents an implementation of <see cref="IProjectionPipeline"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="IProjectionPipeline"/>.
/// </remarks>
/// <param name="projection">The <see cref="EngineProjection"/> the pipeline is for.</param>
/// <param name="sink"><see cref="ISink"/> to use.</param>
/// <param name="changesetStorage"><see cref="IChangesetStorage"/> for storing changesets as they occur.</param>
/// <param name="objectComparer"><see cref="IObjectComparer"/> for comparing objects.</param>
/// <param name="steps">Collection of <see cref="ICanPerformProjectionPipelineStep"/> to perform.</param>
/// <param name="handleLock">Per-projection lock that serializes <see cref="Handle"/> calls across all pipeline instances that share the same projection identifier.</param>
/// <param name="replayScopedCache">The <see cref="IReplayScopedCache"/> whose key-resolution caching is active only for the duration of a replay.</param>
/// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
/// <param name="ensureProvenance">Rechecks database eligibility before accessing the sink.</param>
/// <param name="generation">The <see cref="MaterializationGenerationIdentity"/> the pipeline builds, if it builds a materialization generation.
/// A generation writes only to its own destination and never puts its sink or changesets into replay mode: the destination is its own,
/// so there is nothing to shadow and nothing to rename.</param>
/// <param name="usesBulkWindow">Whether the pipeline opens bulk windows on its sink. A globally scoped projection's sink is shared by every
/// namespace's pipeline, so one namespace ending its catch-up would end or replace another's window; such a pipeline
/// never opens a window and its writes go to the sink per event.</param>
public partial class ProjectionPipeline(
    EngineProjection projection,
    ISink sink,
    IChangesetStorage changesetStorage,
    IObjectComparer objectComparer,
    IEnumerable<ICanPerformProjectionPipelineStep> steps,
    ProjectionHandleLock handleLock,
    IReplayScopedCache replayScopedCache,
    ILogger<ProjectionPipeline> logger,
    Func<Task> ensureProvenance,
    MaterializationGenerationIdentity? generation = null,
    bool usesBulkWindow = true) : IProjectionPipeline
{
    const int MaxFencedAttempts = 8;

    /// <summary>
    /// Serializes the read-modify-write cycle in <see cref="Handle"/>.
    /// </summary>
    /// <remarks>
    /// The pipeline performs a read-modify-write cycle against the sink for each event:
    /// SetInitialState reads the current state, HandleEvent computes the changeset, and
    /// SaveChanges writes the new state back. Multiple concurrent Handle() calls happen when
    /// catch-up or replay runs per-partition steps in parallel, and when a replay evicts the
    /// cached pipeline while already-activated subscribers still hold the old pipeline reference.
    /// For a projection whose key collapses all partitions into a single read-model document
    /// (constant key, joins, or hierarchical parent/child resolution) those calls would race and
    /// produce lost updates or missing parent/child links, so the whole projection is serialized
    /// coarsely. For a purely event-source-keyed projection (<see cref="IProjection.IsEventSourceKeyed"/>)
    /// events for different event source ids target different documents, so handling is striped
    /// per event source id — same-key calls serialize, different-key calls run in parallel. The
    /// lock is owned by <see cref="IProjectionPipelineManager"/> so it survives pipeline eviction:
    /// both the old and new pipeline share the same stripes so the concurrent paths still serialize.
    /// </remarks>
    readonly ProjectionHandleLock _handleLock = handleLock;

    long _activity;
    bool _bulkOpen;
    Action? _sessionOpened;

    ReadModelContainerName ChangesetContainer => generation?.Destination ?? projection.ReadModel.ContainerName;

    /// <inheritdoc/>
    public long GetActivity() => Interlocked.Read(ref _activity);

    /// <inheritdoc/>
    public bool IsPinned() => Volatile.Read(ref _isReplaying) || Volatile.Read(ref _bulkOpen);

    /// <inheritdoc/>
    public void ObserveSessions(Action onSessionOpened) => _sessionOpened = onSessionOpened;

    /// <inheritdoc/>
    public async Task BeginReplay(ReplayContext context)
    {
        await EnsureProvenance();
        Interlocked.Increment(ref _activity);
        ClearPreparedPage();
        _isReplaying = true;
        _sessionOpened?.Invoke();
        replayScopedCache.BeginReplaySession();
        if (generation is not null)
        {
            return;
        }

        await changesetStorage.BeginReplay(ChangesetContainer);
        await sink.BeginReplay(context);
    }

    /// <inheritdoc/>
    public async Task ResumeReplay(ReplayContext context)
    {
        await EnsureProvenance();
        Interlocked.Increment(ref _activity);
        ClearPreparedPage();
        _isReplaying = true;
        _sessionOpened?.Invoke();
        replayScopedCache.BeginReplaySession();
        if (generation is null) await sink.ResumeReplay(context);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<FailedPartition>> EndReplay(ReplayContext context)
    {
        await EnsureProvenance();
        ClearPreparedPage();
        _isReplaying = false;
        replayScopedCache.EndReplaySession();
        if (generation is not null)
        {
            return [];
        }

        // The changeset storage leaves replay whether or not the sink reported failed partitions: those are
        // returned for the observer to record and retry, and the replay is over regardless.
        try
        {
            return (await sink.EndReplay(context)).ToArray();
        }
        finally
        {
            await changesetStorage.EndReplay(ChangesetContainer);
        }
    }

    /// <inheritdoc/>
    public async Task LeaveReplay()
    {
        ClearPreparedPage();
        _isReplaying = false;
        replayScopedCache.EndReplaySession();
        if (generation is not null)
        {
            return;
        }

        try
        {
            await sink.LeaveReplay();
        }
        finally
        {
            await changesetStorage.EndReplay(ChangesetContainer);
        }
    }

    /// <inheritdoc/>
    public async Task BeginBulk()
    {
        await EnsureProvenance();
        Interlocked.Increment(ref _activity);
        Volatile.Write(ref _bulkOpen, true);
        _sessionOpened?.Invoke();
        if (usesBulkWindow) await sink.BeginBulk();
    }

    /// <inheritdoc/>
    public async Task ResumeBulk()
    {
        await EnsureProvenance();
        Interlocked.Increment(ref _activity);
        Volatile.Write(ref _bulkOpen, true);
        _sessionOpened?.Invoke();
        if (usesBulkWindow) await sink.ResumeBulk();
    }

    /// <inheritdoc/>
    public async Task<BulkFlushResult> FlushBulk()
    {
        await EnsureProvenance();
        return usesBulkWindow ? await sink.FlushBulk() : new BulkFlushResult(null, []);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<FailedPartition>> EndBulk()
    {
        await EnsureProvenance();
        ClearPreparedPage();
        Volatile.Write(ref _bulkOpen, false);
        return usesBulkWindow ? await sink.EndBulk() : [];
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<FailedPartition>> EndBulk(CancellationToken cancellationToken)
    {
        await EnsureProvenance();
        ClearPreparedPage();
        Volatile.Write(ref _bulkOpen, false);
        return usesBulkWindow ? await sink.EndBulk(cancellationToken) : [];
    }

    /// <inheritdoc/>
    public async Task<ProjectionEventContext> Handle(AppendedEvent @event)
    {
        await EnsureProvenance();
        Interlocked.Increment(ref _activity);

        // A purely event-source-keyed projection resolves every event's key to its own event source id, so striping
        // on the event source id is striping on the resolved key: same-key handling serializes, different-key
        // handling runs in parallel. Any projection that can collapse partitions keeps the coarse lock.
        using var handleScope = projection.IsEventSourceKeyed
            ? await _handleLock.AcquireFor(@event.Context.EventSourceId)
            : await _handleLock.AcquireCoarse();

        logger.StartingPipeline(@event.Context.SequenceNumber);

        // A write made with a revision check that finds the instance changed has written nothing: read it again and recompute.
        var attempts = projection.SupportsFencedGlobalWrites ? MaxFencedAttempts : 1;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var context = await PerformSteps(@event);
                logger.CompletedAllSteps(@event.Context.SequenceNumber);
                return context;
            }
            catch (SinkWriteConflictException) when (attempt < attempts)
            {
                await Task.Delay(Random.Shared.Next(1, 10 * attempt));
            }
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<(Key Key, ExpandoObject State)>> ReadCurrent(IReadOnlyCollection<Key> keys)
    {
        await EnsureProvenance();
        // Whatever is being handled right now has to land before the state is read, so the read never precedes a write it follows.
        using var handleScope = await _handleLock.AcquireCoarse();
        var states = new List<(Key Key, ExpandoObject State)>(keys.Count);
        foreach (var key in keys)
        {
            if (await sink.FindOrDefault(key) is { } state)
            {
                states.Add((key, state));
            }
        }

        return states;
    }

    async Task<ProjectionEventContext> PerformSteps(AppendedEvent @event)
    {
        var context = ProjectionEventContext.Empty(objectComparer, projection.ForProjection(@event)) with
        {
            OperationType = projection.GetOperationTypeFor(@event.Context.EventType),
            GenerationDelivery = projection.GenerationDelivery,
        };

        foreach (var step in steps)
        {
            try
            {
                context = await step.Perform(projection, context);
            }
            catch (Exception ex)
            {
                logger.ErrorPerformingStep(ex, step.GetType(), @event.Context.SequenceNumber);
                throw;
            }
        }

        return context;
    }
}
