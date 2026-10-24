// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents a step for reindexing changed constraint indexes.
/// </summary>
/// <param name="state"><see cref="IPersistentState{TState}"/> for managing state of the job step.</param>
/// <param name="throttle">The <see cref="IJobStepThrottle"/> for limiting parallel execution.</param>
/// <param name="storage"><see cref="IStorage"/> for storage access.</param>
/// <param name="grainFactory">The thread-safe grain factory for the off-activation job worker.</param>
/// <param name="complianceManager"><see cref="IJsonSchemaMetadataManager"/> for releasing (decrypting) PII before hashing.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between ExpandoObject and JsonObject.</param>
/// <param name="logger">The logger.</param>
/// <remarks>
/// Closing constraints rebuild only when explicitly named by the job. Their transitions are staged before
/// writing differences. If a redacted property-sourced closing or reopening event has no recoverable scope in the
/// existing snapshot, that entire owner's rows stay untouched and a structured warning identifies the
/// constraint and event position. Other constraints still rebuild; no reopening tombstones are retained.
/// </remarks>
public partial class ReindexConstraintsStep(
    [PersistentState(nameof(ReindexConstraintsStepState), Cratis.Orleans.WellKnownGrainStorageProviders.JobSteps)]
    IPersistentState<ReindexConstraintsStepState> state,
    IJobStepThrottle throttle,
    IStorage storage,
    IGrainFactory grainFactory,
    IJsonSchemaMetadataManager complianceManager,
    IExpandoObjectConverter expandoObjectConverter,
    ILogger<ReindexConstraintsStep> logger) : JobStep<ReindexConstraintsRequest, object, ReindexConstraintsStepState>(state, throttle, logger), IReindexConstraintsStep
{
    /// <summary>
    /// Reindexes a single event against a single constraint definition using the released (plaintext) content.
    /// </summary>
    /// <param name="definition">The <see cref="UniqueConstraintDefinition"/> being reindexed.</param>
    /// <param name="event">The <see cref="AppendedEvent"/> being processed.</param>
    /// <param name="content">The released (plaintext) content of the event.</param>
    /// <param name="seen">The already-cleared scopes, each paired with an unspecified event source.</param>
    /// <param name="validator">The <see cref="UniqueConstraintValidator"/> that updates the index.</param>
    /// <param name="uniqueConstraintsStorage">The <see cref="IUniqueConstraintsStorage"/> to update.</param>
    /// <param name="beforeWrite">Called before the first write to a scope.</param>
    /// <param name="personalProperties">The event's top-level properties that carry personal data.</param>
    /// <param name="beforeClaim">Receives the claimed index value before the update.</param>
    /// <returns>Awaitable task.</returns>
    internal static async Task ReindexEvent(
        UniqueConstraintDefinition definition,
        AppendedEvent @event,
        ExpandoObject content,
        HashSet<(EventSourceId EventSourceId, string ScopeKey)> seen,
        UniqueConstraintValidator validator,
        IUniqueConstraintsStorage uniqueConstraintsStorage,
        Action? beforeWrite = null,
        IReadOnlyCollection<string>? personalProperties = null,
        Action<string>? beforeClaim = null)
    {
        var scopeKey = definition.Scope.BuildScopeKey(
            @event.Context.EventSourceType,
            @event.Context.EventStreamType,
            @event.Context.EventStreamId);

        // One step owns the complete sequence for every changed definition. Both retention modes must clear
        // the whole scope before its first event is replayed, not as each owner is encountered: a later
        // owner may still hold a stale value that an earlier owner claimed in the history being rebuilt.
        if (seen.Add((EventSourceId.Unspecified, scopeKey)))
        {
            beforeWrite?.Invoke();
            await uniqueConstraintsStorage.ClearValues(definition, scopeKey);
        }

        var context = ConstraintPersonalData.ContextFor(@event, content, personalProperties ?? [], [validator]);

        // A value-carrying event whose covered property released to an empty value means the subject's
        // encryption key was erased (GDPR right-to-erasure) and the plaintext is permanently unreadable.
        // Skip re-indexing so every erased subject does not collide on the hash of an empty value; the
        // claim simply stays released. Removal and unsupported events fall through to Update, which
        // releases or ignores the claim as appropriate.
        if (StoredConstraintEventContent.IsErased(definition, context))
        {
            return;
        }

        if (beforeClaim is not null && !definition.RemovedWith.Contains(context.EventTypeId) && definition.SupportsEventType(context.EventTypeId))
        {
            var properties = definition.GetPropertiesAndValues(context).ToList();
            if (properties.Count > 0)
            {
                beforeClaim(properties.GetValue(definition.IgnoreCasing));
            }
        }

        await context.Update(@event.Context.SequenceNumber);
    }

    /// <inheritdoc/>
    protected override Task<Result<PrepareJobStepError>> PrepareStep(ReindexConstraintsRequest request) =>
        Task.FromResult(Result.Success<PrepareJobStepError>());

    /// <inheritdoc/>
    protected override ValueTask InitializeState(ReindexConstraintsRequest request)
    {
        State.EventSequenceId = request.EventSequenceId;
        State.Changes = request.Changes.ToList();
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    protected override async ValueTask<object?> CreateCancelledResultFromCurrentState(ReindexConstraintsStepState currentState)
    {
        try
        {
            var runId = new ConstraintIndexRebuildRunId(this.GetPrimaryKey(out var key));
            var jobStepKey = (JobStepKey)key!;
            var definitions = await storage.GetEventStore(jobStepKey.Scope).Constraints.GetDefinitions();
            var names = definitions.Where(definition => definition.AppliesTo(currentState.EventSequenceId) && currentState.Changes.Any(change => change.Name == definition.Name && (definition is ClosesStreamConstraintDefinition || (definition is UniqueConstraintDefinition && change.RequiresReindex)))).Select(definition => definition.Name).Distinct().ToArray();
            if (names.Length > 0)
            {
                await ReportRebuilt(grainFactory.GetEventSequence(currentState.EventSequenceId, jobStepKey.Scope, jobStepKey.Namespace), runId, names, ConstraintIndexRebuildOutcome.ProgressUnknown);
            }
        }
        catch (Exception error)
        {
            // No confirmed terminal delivery: retain the grain's conservative rebuilding mark.
            logger.ConstraintIndexRebuildCancellationHandoffFailed(error);
        }
        return null;
    }

    /// <inheritdoc/>
    protected override async Task<Catch<JobStepResult>> PerformStep(ReindexConstraintsStepState currentState, CancellationToken cancellationToken)
    {
        try
        {
            var runId = new ConstraintIndexRebuildRunId(this.GetPrimaryKey(out var key), Guid.NewGuid(), DateTimeOffset.UtcNow);
            var jobStepKey = (JobStepKey)key!;

            var eventStoreStorage = storage.GetEventStore(jobStepKey.Scope);
            var namespaceStorage = eventStoreStorage.GetNamespace(jobStepKey.Namespace);
            var uniqueConstraintsStorage = namespaceStorage.GetUniqueConstraintsStorage(currentState.EventSequenceId);

            var allConstraintDefinitions = (await eventStoreStorage.Constraints.GetDefinitions()).ToArray();
            var closingDefinitions = ResolveClosingDefinitions(currentState, allConstraintDefinitions);
            var closingReindexer = new ClosesStreamConstraintReindexer(namespaceStorage.GetClosedStreamsConstraints(currentState.EventSequenceId), logger);
            var changedDefinitions = ResolveChangedDefinitions(currentState, allConstraintDefinitions);

            if (changedDefinitions.Length == 0 && closingDefinitions.Length == 0)
            {
                return JobStepResult.Succeeded(null);
            }

            var names = changedDefinitions.Select(definition => definition.Name).Concat(closingDefinitions.Select(definition => definition.Name)).Distinct().ToArray();
            var sequence = grainFactory.GetEventSequence(currentState.EventSequenceId, jobStepKey.Scope, jobStepKey.Namespace);
            var progress = new RebuildProgress();
            try
            {
                await sequence.ConstraintIndexRebuildStarting(runId, names);
                await closingReindexer.Initialize(closingDefinitions);

                var tracking = CreateTracking(changedDefinitions, uniqueConstraintsStorage, progress);
                var storedContent = new StoredConstraintEventContent(jobStepKey.Scope, jobStepKey.Namespace, eventStoreStorage.EventTypes, complianceManager, expandoObjectConverter);
                await ReindexStoredEvents(namespaceStorage.GetEventSequence(currentState.EventSequenceId), storedContent, closingReindexer, tracking, cancellationToken);
                await closingReindexer.Commit(cancellationToken, () => progress.WroteAnything = true);
                if (!await ReportCollisions(progress, cancellationToken))
                {
                    return JobStepResult.Failed(PerformJobStepError.CancelledWithNoResult());
                }

                return JobStepResult.Succeeded(null);
            }
            finally
            {
                var outcome = !progress.WroteAnything ? ConstraintIndexRebuildOutcome.NothingWritten : new(true, progress.ProcessedThrough);
                await ReportRebuilt(sequence, runId, names, outcome);
            }
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    static ClosesStreamConstraintDefinition[] ResolveClosingDefinitions(ReindexConstraintsStepState currentState, IConstraintDefinition[] allConstraintDefinitions)
    {
        var requestedNames = currentState.Changes.Select(change => change.Name).ToHashSet();
        return allConstraintDefinitions.OfType<ClosesStreamConstraintDefinition>()
            .Where(definition => requestedNames.Contains(definition.Name) && definition.AppliesTo(currentState.EventSequenceId)).ToArray();
    }

    static UniqueConstraintDefinition[] ResolveChangedDefinitions(ReindexConstraintsStepState currentState, IConstraintDefinition[] allConstraintDefinitions)
    {
        var constraintsByName = allConstraintDefinitions
            .OfType<UniqueConstraintDefinition>()
            .ToDictionary(_ => _.Name);

        return currentState.Changes
            .Where(_ => _.RequiresReindex)
            .Select(_ => constraintsByName.GetValueOrDefault(_.Name))
            .Where(_ => _ is not null)
            .Cast<UniqueConstraintDefinition>()

            // A constraint is only indexed for the event sequences it applies to, so a reindex must honor the
            // same scope or it would rebuild an index that appends never maintain.
            .Where(_ => _.AppliesTo(currentState.EventSequenceId))
            .ToArray();
    }

    static RebuildTracking CreateTracking(UniqueConstraintDefinition[] changedDefinitions, IUniqueConstraintsStorage uniqueConstraintsStorage, RebuildProgress progress) => new(
        changedDefinitions,
        changedDefinitions.ToDictionary(_ => _.Name, _ => new HashSet<(EventSourceId EventSourceId, string ScopeKey)>()),
        changedDefinitions.ToDictionary(_ => _.Name, _ => new UniqueConstraintValidator(_, uniqueConstraintsStorage)),
        uniqueConstraintsStorage,
        progress);

    async Task ReindexStoredEvents(
        IEventSequenceStorage eventSequenceStorage,
        StoredConstraintEventContent storedContent,
        ClosesStreamConstraintReindexer closingReindexer,
        RebuildTracking tracking,
        CancellationToken cancellationToken)
    {
        // GetFromSequenceNumber supplies an ascending sequence-number cursor in every provider. Consume
        // its batches and events sequentially so releases precede subsequent claims, including on retry.
        using var cursor = await eventSequenceStorage.GetFromSequenceNumber(EventSequenceNumber.First, cancellationToken: cancellationToken);
        while (await cursor.MoveNext())
        {
            foreach (var @event in cursor.Current)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Constraint hashes must be derived from the original plaintext, so release (decrypt) any
                // PII before establishing the validation context. The append-time index write already uses
                // plaintext; reindexing must match it or a rebuilt PII index would diverge from new appends.
                var content = await storedContent.Release(@event, cancellationToken);
                closingReindexer.Include(@event, content);
                var personalProperties = await storedContent.PersonalPropertiesOf(@event, cancellationToken);
                await ReindexDefinitions(tracking, @event, content, personalProperties);
                tracking.Progress.ProcessedThrough = @event.Context.SequenceNumber;
            }
        }
    }

    async Task ReindexDefinitions(RebuildTracking tracking, AppendedEvent @event, ExpandoObject content, IReadOnlyCollection<string> personalProperties)
    {
        foreach (var definition in tracking.Definitions)
        {
            string? claimedValue = null;
            try
            {
                await ReindexEvent(definition, @event, content, tracking.Seen[definition.Name], tracking.Validators[definition.Name], tracking.Storage, () => tracking.Progress.WroteAnything = true, personalProperties, value => claimedValue = value);
            }
            catch (DuplicateUniqueConstraintValue error)
            {
                tracking.Progress.RecordCollision(definition.Name, claimedValue, @event.Context.SequenceNumber);
                logger.SkippingDuplicateUniqueConstraintClaim(definition.Name, @event.Context.EventSourceId, @event.Context.SequenceNumber, error);
            }
        }
    }

    async Task ReportRebuilt(IEventSequence sequence, ConstraintIndexRebuildRunId runId, ConstraintName[] names, ConstraintIndexRebuildOutcome outcome)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await sequence.ConstraintIndexRebuilt(runId, names, outcome);
                return;
            }
            catch (Exception error)
            {
                if (attempt == 4)
                {
                    logger.ConstraintIndexRebuildHandoffFailed(error);
                    throw;
                }
                await Task.Delay(TimeSpan.FromSeconds(1 << attempt), CancellationToken.None);
            }
        }
    }

    sealed record RebuildTracking(
        UniqueConstraintDefinition[] Definitions,
        Dictionary<ConstraintName, HashSet<(EventSourceId EventSourceId, string ScopeKey)>> Seen,
        Dictionary<ConstraintName, UniqueConstraintValidator> Validators,
        IUniqueConstraintsStorage Storage,
        RebuildProgress Progress);
}
