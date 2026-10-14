// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Events.Constraints;
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
/// <param name="complianceManager"><see cref="IJsonSchemaMetadataManager"/> for releasing (decrypting) PII before hashing.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between ExpandoObject and JsonObject.</param>
/// <param name="logger">The logger.</param>
/// <remarks>
/// Closing constraints rebuild only when explicitly named by the job. Their transitions are staged before
/// deleting rows. If a redacted property-sourced closing or reopening event has no recoverable scope in the
/// existing snapshot, that entire owner's rows stay untouched and a structured warning identifies the
/// constraint and event position. Other constraints still rebuild; no reopening tombstones are retained.
/// </remarks>
public class ReindexConstraintsStep(
    [PersistentState(nameof(ReindexConstraintsStepState), Cratis.Orleans.WellKnownGrainStorageProviders.JobSteps)]
    IPersistentState<ReindexConstraintsStepState> state,
    IJobStepThrottle throttle,
    IStorage storage,
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
    /// <returns>Awaitable task.</returns>
    internal static async Task ReindexEvent(
        UniqueConstraintDefinition definition,
        AppendedEvent @event,
        ExpandoObject content,
        HashSet<(EventSourceId EventSourceId, string ScopeKey)> seen,
        UniqueConstraintValidator validator,
        IUniqueConstraintsStorage uniqueConstraintsStorage)
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
            await uniqueConstraintsStorage.ClearValues(definition, scopeKey);
        }

        var context = new ConstraintValidationContext(
            [validator],
            @event.Context.EventSourceId,
            @event.Context.EventType.Id,
            content,
            @event.Context.EventSourceType,
            @event.Context.EventStreamType,
            @event.Context.EventStreamId);

        // A value-carrying event whose covered property released to an empty value means the subject's
        // encryption key was erased (GDPR right-to-erasure) and the plaintext is permanently unreadable.
        // Skip re-indexing so every erased subject does not collide on the hash of an empty value; the
        // claim simply stays released. Removal and unsupported events fall through to Update, which
        // releases or ignores the claim as appropriate.
        if (StoredConstraintEventContent.IsErased(definition, context))
        {
            return;
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
    protected override ValueTask<object?> CreateCancelledResultFromCurrentState(ReindexConstraintsStepState currentState) =>
        ValueTask.FromResult<object?>(null);

    /// <inheritdoc/>
    protected override async Task<Catch<JobStepResult>> PerformStep(ReindexConstraintsStepState currentState, CancellationToken cancellationToken)
    {
        try
        {
            _ = this.GetPrimaryKey(out var key);
            var jobStepKey = (JobStepKey)key!;

            var eventStoreStorage = storage.GetEventStore(jobStepKey.Scope);
            var namespaceStorage = eventStoreStorage.GetNamespace(jobStepKey.Namespace);
            var eventSequenceStorage = namespaceStorage.GetEventSequence(currentState.EventSequenceId);
            var uniqueConstraintsStorage = namespaceStorage.GetUniqueConstraintsStorage(currentState.EventSequenceId);
            var eventTypesStorage = eventStoreStorage.EventTypes;

            var allConstraintDefinitions = (await eventStoreStorage.Constraints.GetDefinitions()).ToArray();
            var requestedNames = currentState.Changes.Select(change => change.Name).ToHashSet();
            var closingDefinitions = allConstraintDefinitions.OfType<ClosesStreamConstraintDefinition>()
                .Where(definition => requestedNames.Contains(definition.Name) && definition.AppliesTo(currentState.EventSequenceId)).ToArray();
            var closingReindexer = new ClosesStreamConstraintReindexer(namespaceStorage.GetClosedStreamsConstraints(currentState.EventSequenceId), logger);
            await closingReindexer.Initialize(closingDefinitions);
            var constraintsByName = allConstraintDefinitions
                .OfType<UniqueConstraintDefinition>()
                .ToDictionary(_ => _.Name);

            var changedDefinitions = currentState.Changes
                .Where(_ => _.RequiresReindex)
                .Select(_ => constraintsByName.GetValueOrDefault(_.Name))
                .Where(_ => _ is not null)
                .Cast<UniqueConstraintDefinition>()

                // A constraint is only indexed for the event sequences it applies to, so a reindex must honor the
                // same scope or it would rebuild an index that appends never maintain.
                .Where(_ => _.AppliesTo(currentState.EventSequenceId))
                .ToArray();

            if (changedDefinitions.Length == 0 && closingDefinitions.Length == 0)
            {
                return JobStepResult.Succeeded(null);
            }

            var seenConstraintEntries = changedDefinitions.ToDictionary(_ => _.Name, _ => new HashSet<(EventSourceId EventSourceId, string ScopeKey)>());
            var validators = changedDefinitions.ToDictionary(_ => _.Name, _ => new UniqueConstraintValidator(_, uniqueConstraintsStorage));
            var storedContent = new StoredConstraintEventContent(jobStepKey.Scope, jobStepKey.Namespace, eventTypesStorage, complianceManager, expandoObjectConverter);

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

                    foreach (var definition in changedDefinitions)
                    {
                        try
                        {
                            await ReindexEvent(definition, @event, content, seenConstraintEntries[definition.Name], validators[definition.Name], uniqueConstraintsStorage);
                        }
                        catch (DuplicateUniqueConstraintValue error)
                        {
                            logger.SkippingDuplicateUniqueConstraintClaim(definition.Name, @event.Context.EventSourceId, @event.Context.SequenceNumber, error);
                        }
                    }
                }
            }

            await closingReindexer.Commit(cancellationToken);

            return JobStepResult.Succeeded(null);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
