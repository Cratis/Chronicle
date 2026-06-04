// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Monads;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents a step for reindexing changed constraint indexes.
/// </summary>
/// <param name="state"><see cref="IPersistentState{TState}"/> for managing state of the job step.</param>
/// <param name="throttle">The <see cref="IJobStepThrottle"/> for limiting parallel execution.</param>
/// <param name="storage"><see cref="IStorage"/> for storage access.</param>
/// <param name="logger">The logger.</param>
public class ReindexConstraintsStep(
    [PersistentState(nameof(ReindexConstraintsStepState), WellKnownGrainStorageProviders.JobSteps)]
    IPersistentState<ReindexConstraintsStepState> state,
    IJobStepThrottle throttle,
    IStorage storage,
    ILogger<ReindexConstraintsStep> logger) : JobStep<ReindexConstraintsRequest, object, ReindexConstraintsStepState>(state, throttle, logger), IReindexConstraintsStep
{
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

            var namespaceStorage = storage.GetEventStore(jobStepKey.EventStore).GetNamespace(jobStepKey.Namespace);
            var eventSequenceStorage = namespaceStorage.GetEventSequence(currentState.EventSequenceId);
            var uniqueConstraintsStorage = namespaceStorage.GetUniqueConstraintsStorage(currentState.EventSequenceId);

            var allConstraintDefinitions = await storage.GetEventStore(jobStepKey.EventStore).Constraints.GetDefinitions();
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

            using var cursor = await eventSequenceStorage.GetFromSequenceNumber(EventSequenceNumber.First, cancellationToken: cancellationToken);
            while (await cursor.MoveNext())
            {
                foreach (var @event in cursor.Current)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    foreach (var definition in changedDefinitions)
                    {
                        var scopeKey = definition.Scope.BuildScopeKey(
                            @event.Context.EventSourceType,
                            @event.Context.EventStreamType,
                            @event.Context.EventStreamId);

                        var seen = seenConstraintEntries[definition.Name];
                        if (seen.Add((@event.Context.EventSourceId, scopeKey)))
                        {
                            await uniqueConstraintsStorage.Remove(@event.Context.EventSourceId, definition.Name, scopeKey);
                        }

                        var validator = validators[definition.Name];
                        var context = new ConstraintValidationContext(
                            [validator],
                            @event.Context.EventSourceId,
                            @event.Context.EventType.Id,
                            @event.Content,
                            @event.Context.EventSourceType,
                            @event.Context.EventStreamType,
                            @event.Context.EventStreamId);

                        await context.Update(@event.Context.SequenceNumber);
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
}
