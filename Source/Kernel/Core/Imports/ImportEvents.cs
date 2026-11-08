// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents the job that validates an import as a whole and commits it in ordered chunks (decision 0023).
/// </summary>
/// <remarks>
/// One step does the work in order: validation must see the chunks in staging order, and the commit must append them
/// in staging order. The step resumes from the import's checkpoint after a restart.
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
}
