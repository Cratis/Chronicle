// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Monads;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventSequences.Copy;

/// <summary>
/// Represents the job step that copies the matching events of the source event source to the target event source.
/// </summary>
/// <param name="state">The persisted step state.</param>
/// <param name="throttle">The <see cref="IJobStepThrottle"/>.</param>
/// <param name="storage">The <see cref="IStorage"/> to read the source events from.</param>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for reaching the event sequence to append to.</param>
/// <param name="metadataManager">The <see cref="IJsonSchemaMetadataManager"/> releasing personal data.</param>
/// <param name="converter">The <see cref="IExpandoObjectConverter"/>.</param>
/// <param name="logger">The <see cref="ILogger{CopyEventSourceEventsStep}"/> for logging.</param>
/// <param name="schemaCache">The <see cref="IEventTypeSchemaCache"/> for the stored event content schemas.</param>
public class CopyEventSourceEventsStep(
    [PersistentState(nameof(CopyEventSourceEventsStepState), Cratis.Choreography.WellKnownGrainStorageProviders.JobSteps)]
    IPersistentState<CopyEventSourceEventsStepState> state,
    IJobStepThrottle throttle,
    IStorage storage,
    IGrainFactory grainFactory,
    IJsonSchemaMetadataManager metadataManager,
    IExpandoObjectConverter converter,
    ILogger<CopyEventSourceEventsStep> logger,
    IEventTypeSchemaCache schemaCache) : JobStep<CopyEventSourceEventsRequest, object, CopyEventSourceEventsStepState>(state, throttle, logger), ICopyEventSourceEventsStep
{
    const int BatchSize = 100;
    static readonly CausationType _causationType = new("CopyEventSourceEvents");

    /// <inheritdoc/>
    protected override ValueTask InitializeState(CopyEventSourceEventsRequest request)
    {
        State.EventSequenceId = request.EventSequenceId;
        State.SourceId = request.SourceId;
        State.TargetId = request.TargetId;
        State.EventTypes = request.EventTypes.ToList();
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    protected override ValueTask<object?> CreateCancelledResultFromCurrentState(CopyEventSourceEventsStepState currentState) => ValueTask.FromResult<object?>(null);

    /// <inheritdoc/>
    protected override Task<Result<PrepareJobStepError>> PrepareStep(CopyEventSourceEventsRequest request) => Task.FromResult(Result.Success<PrepareJobStepError>());

    /// <inheritdoc/>
    protected override async Task<Catch<JobStepResult>> PerformStep(CopyEventSourceEventsStepState currentState, CancellationToken cancellationToken)
    {
        try
        {
            var stepId = this.GetPrimaryKey(out var key);
            var jobStepKey = (JobStepKey)key!;
            var eventStoreStorage = storage.GetEventStore(jobStepKey.Scope);
            var sequenceStorage = eventStoreStorage.GetNamespace(jobStepKey.Namespace).GetEventSequence(currentState.EventSequenceId);
            var sequence = grainFactory.GetEventSequence(currentState.EventSequenceId, jobStepKey.Scope, jobStepKey.Namespace);
            var content = new StoredConstraintEventContent(jobStepKey.Scope, jobStepKey.Namespace, eventStoreStorage.EventTypes, metadataManager, converter, schemaCache);
            var eventTypes = currentState.EventTypes.Count == 0
                ? null
                : currentState.EventTypes.Select(_ => new EventType(_, EventTypeGeneration.First)).ToArray();
            var causation = new[]
            {
                new Causation(DateTimeOffset.UtcNow, _causationType, new Dictionary<string, string>
                {
                    ["jobId"] = jobStepKey.JobId.ToString(),
                    ["sourceId"] = currentState.SourceId.Value,
                    ["targetId"] = currentState.TargetId.Value
                })
            };

            using var cursor = await sequenceStorage.GetFromSequenceNumber(
                EventSequenceNumber.First,
                eventSourceId: currentState.SourceId,
                eventTypes: eventTypes,
                cancellationToken: cancellationToken);

            var batch = new List<(EventToAppend Event, IdempotencyKey Key)>();
            while (await cursor.MoveNext())
            {
                foreach (var @event in cursor.Current)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return JobStepResult.Failed(PerformJobStepError.CancelledWithNoResult());
                    }

                    batch.Add(await ToCopy(@event, currentState.TargetId, jobStepKey, content, cancellationToken));
                    if (batch.Count >= BatchSize)
                    {
                        await Append(sequence, batch, causation, currentState);
                        batch.Clear();
                    }
                }
            }

            if (batch.Count > 0)
            {
                await Append(sequence, batch, causation, currentState);
            }

            return JobStepResult.Succeeded(null);
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    async Task<(EventToAppend Event, IdempotencyKey Key)> ToCopy(
        AppendedEvent @event,
        EventSourceId targetId,
        JobStepKey jobStepKey,
        StoredConstraintEventContent content,
        CancellationToken cancellationToken)
    {
        var context = @event.Context;
        var released = await content.Release(@event, cancellationToken);
        var schema = await storage.GetEventStore(jobStepKey.Scope).EventTypes.GetFor(context.EventType.Id, context.EventType.Generation);
        var json = converter.ToJsonObject(released, schema.Schema);

        // A subject that was only the source's identity follows the copy to the target; an explicit subject stays.
        var subject = context.SubjectIsEventSourceId ? new Subject(targetId.Value) : context.Subject;
        var toAppend = new EventToAppend(
            context.EventSourceType,
            targetId,
            context.EventStreamType,
            context.EventStreamId,
            context.EventType,
            context.Tags,
            json,
            context.Occurred,
            subject)
        {
            NamedTags = context.NamedTags.ToArray()
        };

        return (toAppend, new IdempotencyKey($"copy:{jobStepKey.JobId}:{context.SequenceNumber.Value}"));
    }

    async Task Append(
        IEventSequence sequence,
        List<(EventToAppend Event, IdempotencyKey Key)> batch,
        Causation[] causation,
        CopyEventSourceEventsStepState currentState)
    {
        logger.CopyingBatch(batch.Count, currentState.SourceId, currentState.TargetId);
        var result = await sequence.AppendManyWithIdempotencyKeys(
            batch.Select(_ => _.Event).ToArray(),
            batch.Select(_ => (IdempotencyKey?)_.Key).ToArray(),
            CorrelationId.New(),
            causation,
            Identity.System,
            new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));

        if (!result.IsSuccess)
        {
            throw new CopyEventSourceEventsFailed(currentState.SourceId, currentState.TargetId, result);
        }
    }
}
