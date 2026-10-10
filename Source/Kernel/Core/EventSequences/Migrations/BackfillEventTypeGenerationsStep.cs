// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventSequences.Migrations;

/// <summary>
/// Performs guarded, add-only backfill on upgraded workers.
/// </summary>
/// <param name="state">The persistent step state.</param>
/// <param name="throttle">The job step throttle.</param>
/// <param name="storage">The cluster storage.</param>
/// <param name="eventTypeMigrations">The plaintext migration engine.</param>
/// <param name="logger">The step logger.</param>
/// <param name="metadataManager">The generation-specific protection manager.</param>
/// <param name="converter">The schema-guided converter.</param>
/// <param name="hashCalculator">The protected content hash calculator.</param>
public class BackfillEventTypeGenerationsStep(
    [PersistentState(nameof(BackfillEventTypeGenerationsStepState), Cratis.Orleans.WellKnownGrainStorageProviders.JobSteps)]
    IPersistentState<BackfillEventTypeGenerationsStepState> state,
    IJobStepThrottle throttle,
    IStorage storage,
    IEventTypeMigrations eventTypeMigrations,
    ILogger<BackfillEventTypeGenerationsStep> logger,
    IJsonSchemaMetadataManager metadataManager,
    IExpandoObjectConverter converter,
    IEventHashCalculator hashCalculator) : JobStep<BackfillEventTypeGenerationsRequest, object, BackfillEventTypeGenerationsStepState>(state, throttle, logger), IBackfillEventTypeGenerationsStep
{
    /// <inheritdoc/>
    protected override ValueTask InitializeState(BackfillEventTypeGenerationsRequest request)
    {
        State.EventTypeId = request.EventTypeId;

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    protected override ValueTask<object?> CreateCancelledResultFromCurrentState(BackfillEventTypeGenerationsStepState currentState) => ValueTask.FromResult<object?>(null);

    /// <inheritdoc/>
    protected override Task<Result<PrepareJobStepError>> PrepareStep(BackfillEventTypeGenerationsRequest request) => Task.FromResult(Result.Success<PrepareJobStepError>());

    /// <inheritdoc/>
    protected override async Task<Catch<JobStepResult>> PerformStep(BackfillEventTypeGenerationsStepState currentState, CancellationToken cancellationToken)
    {
        try
        {
            _ = this.GetPrimaryKey(out var key);
            var jobStepKey = (JobStepKey)key!;
            var backfill = new EventTypeGenerationBackfill(storage, eventTypeMigrations, metadataManager, converter, hashCalculator);

            return await backfill.Perform(jobStepKey.Scope, jobStepKey.Namespace, currentState.EventTypeId, logger, cancellationToken);
        }
        catch (Exception exception)
        {
            return exception;
        }
    }
}
