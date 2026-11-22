// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents the job carrying out a command an operator gave an observer.
/// </summary>
/// <remarks>
/// An operator's command is a call on the observer grain, which also serves the subscriptions of its client; while that is
/// slow the call queues behind them and times out although it was fine. As a job the command is accepted and recorded at
/// once, and its step gives it to the observer as often as it takes. A job found unfinished for the same command joins the
/// one that is already there (see <see cref="ObserverOperatorCommandJobs"/>).
/// </remarks>
public class ObserverOperatorCommandJob : Job<ObserverOperatorCommandRequest, JobState>, IObserverOperatorCommandJob
{
    /// <inheritdoc/>
    protected override Task<IImmutableList<JobStepDetails>> PrepareSteps(ObserverOperatorCommandRequest request) =>
        Task.FromResult<IImmutableList<JobStepDetails>>([CreateStep<IObserverOperatorCommandStep>(request)]);

    /// <inheritdoc/>
    protected override JobDetails GetJobDetails() => $"{Request.Command} {Request.ObserverKey.ObserverId}";

    /// <inheritdoc/>
    protected override Task<bool> CanResume() => Task.FromResult(true);
}
