// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Monads;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents a step for reindexing changed constraint indexes.
/// </summary>
/// <param name="state"><see cref="IPersistentState{TState}"/> for managing state of the job step.</param>
/// <param name="throttle">The <see cref="IJobStepThrottle"/> for limiting parallel execution.</param>
/// <param name="storage"><see cref="IStorage"/> for storage access.</param>
/// <param name="complianceManager"><see cref="IJsonComplianceManager"/> for releasing (decrypting) PII before hashing.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between ExpandoObject and JsonObject.</param>
/// <param name="logger">The logger.</param>
public class ReindexConstraintsStep(
    [PersistentState(nameof(ReindexConstraintsStepState), WellKnownGrainStorageProviders.JobSteps)]
    IPersistentState<ReindexConstraintsStepState> state,
    IJobStepThrottle throttle,
    IStorage storage,
    IJsonComplianceManager complianceManager,
    IExpandoObjectConverter expandoObjectConverter,
    ILogger<ReindexConstraintsStep> logger) : JobStep<ReindexConstraintsRequest, object, ReindexConstraintsStepState>(state, throttle, logger), IReindexConstraintsStep
{
    /// <summary>
    /// Reindexes a single event against a single constraint definition using the released (plaintext) content.
    /// </summary>
    /// <param name="definition">The <see cref="UniqueConstraintDefinition"/> being reindexed.</param>
    /// <param name="event">The <see cref="AppendedEvent"/> being processed.</param>
    /// <param name="content">The released (plaintext) content of the event.</param>
    /// <param name="seen">The set of already-cleared (event source, scope) entries for this definition.</param>
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

        if (seen.Add((@event.Context.EventSourceId, scopeKey)))
        {
            await uniqueConstraintsStorage.Remove(@event.Context.EventSourceId, definition.Name, scopeKey);
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
        if (definition.SupportsEventType(@event.Context.EventType.Id) &&
            @event.Context.EventType.Id != definition.RemovedWith &&
            definition.GetPropertiesAndValues(context).Any(_ => string.IsNullOrEmpty(_.Value)))
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

            var eventStoreStorage = storage.GetEventStore(jobStepKey.EventStore);
            var namespaceStorage = eventStoreStorage.GetNamespace(jobStepKey.Namespace);
            var eventSequenceStorage = namespaceStorage.GetEventSequence(currentState.EventSequenceId);
            var uniqueConstraintsStorage = namespaceStorage.GetUniqueConstraintsStorage(currentState.EventSequenceId);
            var eventTypesStorage = eventStoreStorage.EventTypes;

            var allConstraintDefinitions = await eventStoreStorage.Constraints.GetDefinitions();
            var constraintsByName = allConstraintDefinitions
                .OfType<UniqueConstraintDefinition>()
                .ToDictionary(_ => _.Name);

            var changedDefinitions = currentState.Changes
                .Where(_ => _.RequiresReindex)
                .Select(_ => constraintsByName.GetValueOrDefault(_.Name))
                .Where(_ => _ is not null)
                .Cast<UniqueConstraintDefinition>()
                .ToArray();

            if (changedDefinitions.Length == 0)
            {
                return JobStepResult.Succeeded(null);
            }

            var seenConstraintEntries = changedDefinitions.ToDictionary(_ => _.Name, _ => new HashSet<(EventSourceId EventSourceId, string ScopeKey)>());
            var validators = changedDefinitions.ToDictionary(_ => _.Name, _ => new UniqueConstraintValidator(_, uniqueConstraintsStorage));
            var schemaCache = new Dictionary<EventType, EventTypeSchema>();

            using var cursor = await eventSequenceStorage.GetFromSequenceNumber(EventSequenceNumber.First, cancellationToken: cancellationToken);
            while (await cursor.MoveNext())
            {
                foreach (var @event in cursor.Current)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!schemaCache.TryGetValue(@event.Context.EventType, out var eventSchema))
                    {
                        eventSchema = await eventTypesStorage.GetFor(@event.Context.EventType.Id, @event.Context.EventType.Generation);
                        schemaCache[@event.Context.EventType] = eventSchema;
                    }

                    // Constraint hashes must be derived from the original plaintext, so release (decrypt) any
                    // PII before establishing the validation context. The append-time index write already uses
                    // plaintext; reindexing must match it or a rebuilt PII index would diverge from new appends.
                    var content = await ReleaseContent(jobStepKey.EventStore, jobStepKey.Namespace, @event, eventSchema);

                    foreach (var definition in changedDefinitions)
                    {
                        await ReindexEvent(definition, @event, content, seenConstraintEntries[definition.Name], validators[definition.Name], uniqueConstraintsStorage);
                    }
                }
            }

            return JobStepResult.Succeeded(null);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <summary>
    /// Releases (decrypts) the stored event content so that constraint hashes are computed over plaintext.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the event belongs to.</param>
    /// <param name="eventStoreNamespace">The <see cref="EventStoreNamespaceName"/> the event belongs to.</param>
    /// <param name="event">The <see cref="AppendedEvent"/> whose content should be released.</param>
    /// <param name="eventSchema">The <see cref="EventTypeSchema"/> describing the event content.</param>
    /// <returns>The released (decrypted) content, or the original content when the event carries no compliance metadata.</returns>
    async Task<ExpandoObject> ReleaseContent(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, AppendedEvent @event, EventTypeSchema eventSchema)
    {
        if (!eventSchema.Schema.HasComplianceMetadata())
        {
            return @event.Content;
        }

        var identifier = @event.Context.Subject.IsSet ? @event.Context.Subject.Value : @event.Context.EventSourceId.Value;
        var json = expandoObjectConverter.ToJsonObject(@event.Content, eventSchema.Schema);
        var released = await complianceManager.Release(eventStore, eventStoreNamespace, eventSchema.Schema, identifier, json);
        return expandoObjectConverter.ToExpandoObject(released, eventSchema.Schema);
    }
}
