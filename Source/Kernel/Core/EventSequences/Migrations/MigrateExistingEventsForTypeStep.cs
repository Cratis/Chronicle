// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventSequences.Migrations;

/// <summary>
/// Represents a job step that iterates all existing events of a specific event type
/// and updates their generational content using the current migration definitions.
/// </summary>
/// <param name="state"><see cref="IPersistentState{TState}"/> for managing state of the job step.</param>
/// <param name="throttle">The <see cref="IJobStepThrottle"/> for limiting parallel execution.</param>
/// <param name="storage"><see cref="IStorage"/> for accessing storage for the cluster.</param>
/// <param name="eventTypeMigrations"><see cref="IEventTypeMigrations"/> for migrating event content.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> for serialization.</param>
/// <param name="logger">The <see cref="ILogger{MigrateExistingEventsForTypeStep}"/> for logging.</param>
public class MigrateExistingEventsForTypeStep(
    [PersistentState(nameof(MigrateExistingEventsForTypeStepState), WellKnownGrainStorageProviders.JobSteps)]
    IPersistentState<MigrateExistingEventsForTypeStepState> state,
    IJobStepThrottle throttle,
    IStorage storage,
    IEventTypeMigrations eventTypeMigrations,
    JsonSerializerOptions jsonSerializerOptions,
    ILogger<MigrateExistingEventsForTypeStep> logger) : JobStep<MigrateExistingEventsForTypeRequest, object, MigrateExistingEventsForTypeStepState>(state, throttle, logger), IMigrateExistingEventsForTypeStep
{
    IEventSequenceStorage? _eventSequenceStorage;

    /// <inheritdoc/>
    protected override ValueTask InitializeState(MigrateExistingEventsForTypeRequest request)
    {
        State.EventTypeId = request.EventTypeId;

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    protected override ValueTask<object?> CreateCancelledResultFromCurrentState(MigrateExistingEventsForTypeStepState currentState) =>
        ValueTask.FromResult<object?>(null);

    /// <inheritdoc/>
    protected override Task<Result<PrepareJobStepError>> PrepareStep(MigrateExistingEventsForTypeRequest request) =>
        Task.FromResult(Result.Success<PrepareJobStepError>());

    /// <inheritdoc/>
    protected override async Task<Catch<JobStepResult>> PerformStep(MigrateExistingEventsForTypeStepState currentState, CancellationToken cancellationToken)
    {
        try
        {
            _ = this.GetPrimaryKey(out var key);
            var jobStepKey = (JobStepKey)key!;

            var eventSequenceStorage = GetEventSequenceStorage(jobStepKey);
            var eventTypes = new[] { new EventType(currentState.EventTypeId, EventTypeGeneration.First) };

            using var cursor = await eventSequenceStorage.GetFromSequenceNumber(
                EventSequenceNumber.First,
                eventTypes: eventTypes,
                cancellationToken: cancellationToken);

            while (await cursor.MoveNext())
            {
                foreach (var @event in cursor.Current)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return JobStepResult.Failed(PerformJobStepError.CancelledWithNoResult());
                    }

                    logger.MigratingEvent(@event.Context.SequenceNumber, currentState.EventTypeId);

                    var json = JsonSerializer.Serialize(@event.Content, jsonSerializerOptions);
                    var contentAsJson = JsonNode.Parse(json)?.AsObject() ?? new JsonObject();

                    var migratedContent = await eventTypeMigrations.MigrateToAllGenerations(
                        jobStepKey.EventStore,
                        @event.Context.EventType,
                        contentAsJson);

                    await eventSequenceStorage.ReplaceGenerationContent(
                        @event.Context.SequenceNumber,
                        migratedContent);
                }
            }

            return JobStepResult.Succeeded(null);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    IEventSequenceStorage GetEventSequenceStorage(JobStepKey jobStepKey) =>
        _eventSequenceStorage ??= storage
            .GetEventStore(jobStepKey.EventStore)
            .GetNamespace(jobStepKey.Namespace)
            .GetEventSequence(WellKnownEventSequences.EventLog);
}
