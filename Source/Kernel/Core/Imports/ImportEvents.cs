// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents the job that validates an import as a whole and commits it in ordered chunks (decision 0023).
/// </summary>
/// <remarks>
/// One step does the work in order: validation must see the chunks in staging order, and the commit must append them
/// in staging order. The step resumes from the import's checkpoint after a restart.
/// </remarks>
/// <remarks>
/// A step that is stopped leaves the import committing, to be resumed. When the job ends any other way than
/// successfully, or is removed, the import is failed so it never stays committing with nothing left to commit it.
/// </remarks>
public class ImportEvents : Job<ImportEventsRequest, JobState>, IImportEvents
{
    /// <inheritdoc/>
    protected override Task<IImmutableList<JobStepDetails>> PrepareSteps(ImportEventsRequest request) =>
        Task.FromResult<IImmutableList<JobStepDetails>>([CreateStep<IImportEventsStep>(request)]);

    /// <inheritdoc/>
    protected override JobDetails GetJobDetails() => $"Import {Request.ImportId}";

    /// <inheritdoc/>
    protected override Task<bool> CanResume() => Task.FromResult(true);

    /// <inheritdoc/>
    protected override async Task OnAllStepsCompleted()
    {
        // Also called when the job is removed. A completed or already failed import ignores this.
        if (AllStepsCompletedSuccessfully) return;
        var import = GrainFactory.GetGrain<IImport>(new ImportKey(JobKey.Scope, JobKey.Namespace, Request.ImportId));
        await import.Fail($"The commit job {JobId} ended without completing the import. Committed events stay committed.");
    }
}
