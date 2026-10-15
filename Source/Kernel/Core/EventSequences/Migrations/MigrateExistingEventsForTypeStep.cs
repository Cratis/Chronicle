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
/// Resumes persisted migration steps using guarded, add-only backfill while retaining their wire identity.
/// </summary>
/// <param name="state">The persistent legacy step state.</param>
/// <param name="throttle">The job step throttle.</param>
/// <param name="storage">The cluster storage.</param>
/// <param name="eventTypeMigrations">The plaintext migration engine.</param>
/// <param name="logger">The step logger.</param>
/// <param name="metadataManager">The metadata manager releasing and protecting event content.</param>
/// <param name="converter">The schema-guided converter.</param>
/// <param name="hashCalculator">The protected content hash calculator.</param>
public class MigrateExistingEventsForTypeStep(
    [PersistentState(nameof(MigrateExistingEventsForTypeStepState), Cratis.Orleans.WellKnownGrainStorageProviders.JobSteps)]
    IPersistentState<MigrateExistingEventsForTypeStepState> state,
    IJobStepThrottle throttle,
    IStorage storage,
    IEventTypeMigrations eventTypeMigrations,
    ILogger<MigrateExistingEventsForTypeStep> logger,
    IJsonSchemaMetadataManager metadataManager,
    IExpandoObjectConverter converter,
    IEventHashCalculator hashCalculator) : JobStep<MigrateExistingEventsForTypeRequest, object, MigrateExistingEventsForTypeStepState>(state, throttle, logger), IMigrateExistingEventsForTypeStep
{
    /// <inheritdoc/>
    protected override ValueTask InitializeState(MigrateExistingEventsForTypeRequest request)
    {
        State.EventTypeId = request.EventTypeId;

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    protected override ValueTask<object?> CreateCancelledResultFromCurrentState(MigrateExistingEventsForTypeStepState currentState) => ValueTask.FromResult<object?>(null);

    /// <inheritdoc/>
    protected override Task<Result<PrepareJobStepError>> PrepareStep(MigrateExistingEventsForTypeRequest request) => Task.FromResult(Result.Success<PrepareJobStepError>());

    /// <inheritdoc/>
    protected override async Task<Catch<JobStepResult>> PerformStep(MigrateExistingEventsForTypeStepState currentState, CancellationToken cancellationToken)
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
