// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Jobs;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents a job for reindexing changed constraint indexes.
/// </summary>
public class ReindexConstraints : Job<ReindexConstraintsRequest, JobState>, IReindexConstraints
{
    /// <inheritdoc/>
    protected override Task<IImmutableList<JobStepDetails>> PrepareSteps(ReindexConstraintsRequest request) =>
        Task.FromResult<IImmutableList<JobStepDetails>>(
            request.Changes.Count == 0
                ? []
                : [CreateStep<IReindexConstraintsStep>(request)]);

    /// <inheritdoc/>
    protected override Task OnFailedToPrepare()
    {
        JobFailures.FailedToPrepare(ServiceProvider, "reindex_constraints");
        return base.OnFailedToPrepare();
    }

    /// <inheritdoc/>
    protected override Task OnAllStepsCompleted()
    {
        JobFailures.Completed(ServiceProvider, "reindex_constraints", State.Progress);
        return base.OnAllStepsCompleted();
    }

    /// <inheritdoc/>
    protected override JobDetails GetJobDetails() => $"Reindex constraints for {Request.EventSequenceId}";

    /// <inheritdoc/>
    protected override Task<bool> CanResume() => Task.FromResult(true);
}
