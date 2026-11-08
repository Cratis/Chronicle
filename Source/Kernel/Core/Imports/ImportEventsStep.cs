// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Monads;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents the step that validates an import as a whole and then commits it, chunk by chunk, in staging order.
/// </summary>
/// <param name="state"><see cref="IPersistentState{TState}"/> for the step.</param>
/// <param name="throttle">The <see cref="IJobStepThrottle"/>.</param>
/// <param name="storage">The <see cref="IStorage"/> holding the staged chunks.</param>
/// <param name="grainFactory">The thread-safe grain factory for the off-activation job worker.</param>
/// <param name="options">The <see cref="ChronicleOptions"/>.</param>
/// <param name="logger">The logger.</param>
/// <remarks>
/// Validation holds no lock: it runs chunk by chunk on the event sequence, which keeps the import's claims between
/// calls. The commit appends a chunk at a time and records each settled chunk on the import before the next, which is
/// the checkpoint a resumed step continues from. A chunk interrupted before its checkpoint is committed again, and the
/// event sequence recognizes the events of it that are already in the log, so nothing is appended twice.
/// </remarks>
public class ImportEventsStep(
    [PersistentState(nameof(ImportEventsStepState), Cratis.Choreography.WellKnownGrainStorageProviders.JobSteps)]
    IPersistentState<ImportEventsStepState> state,
    IJobStepThrottle throttle,
    IStorage storage,
    IGrainFactory grainFactory,
    IOptions<ChronicleOptions> options,
    ILogger<ImportEventsStep> logger) : JobStep<ImportEventsRequest, object, ImportEventsStepState>(state, throttle, logger), IImportEventsStep
{
    const int MaxAttempts = 10;

    /// <inheritdoc/>
    protected override Task<Result<PrepareJobStepError>> PrepareStep(ImportEventsRequest request) =>
        Task.FromResult(Result.Success<PrepareJobStepError>());

    /// <inheritdoc/>
    protected override ValueTask InitializeState(ImportEventsRequest request)
    {
        State.ImportId = request.ImportId;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    protected override ValueTask<object?> CreateCancelledResultFromCurrentState(ImportEventsStepState currentState) => ValueTask.FromResult<object?>(null);

    /// <inheritdoc/>
    protected override async Task<Catch<JobStepResult>> PerformStep(ImportEventsStepState currentState, CancellationToken cancellationToken)
    {
        try
        {
            this.GetPrimaryKey(out var key);
            var jobStepKey = (JobStepKey)key!;
            var importKey = new ImportKey(jobStepKey.Scope, jobStepKey.Namespace, currentState.ImportId);
            var work = new ImportCommitWork(
                importKey,
                grainFactory.GetGrain<IImport>(importKey),
                storage.GetEventStore(importKey.EventStore).GetNamespace(importKey.Namespace).Imports,
                eventSequenceId => grainFactory.GetEventSequence(eventSequenceId, importKey.EventStore, importKey.Namespace),
                options.Value.Imports.CommitChunkSize,
                MaxAttempts);
            await work.Run(cancellationToken);
            return JobStepResult.Succeeded(null);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
