// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Reports the progress of one observer job activation to the <see cref="IObserverJobGauges"/>.
/// </summary>
/// <param name="gauges">The <see cref="IObserverJobGauges"/>, if any.</param>
/// <param name="jobType">The <see cref="ObserverJobGaugeType"/> of the job.</param>
internal sealed class ObserverJobGaugeReporter(IObserverJobGauges? gauges, ObserverJobGaugeType jobType)
{
    ObserverJobGaugeEntry? _entry;

    /// <summary>
    /// Reports the job as running with its current progress, registering it on first report.
    /// </summary>
    /// <param name="jobId">The <see cref="JobId"/> of the job.</param>
    /// <param name="observerKey">The <see cref="ObserverKey"/> of the observer the job works for.</param>
    /// <param name="progress">The <see cref="JobProgress"/> of the job.</param>
    internal void Report(JobId jobId, ObserverKey observerKey, JobProgress progress)
    {
        if (gauges is null) return;
        _entry ??= gauges.Register(jobId, observerKey, jobType);
        _entry?.Publish(progress.SuccessfulSteps + progress.FailedSteps, progress.TotalSteps);
    }

    /// <summary>
    /// Reports progress only when the job is already reported as running.
    /// </summary>
    /// <param name="progress">The <see cref="JobProgress"/> of the job.</param>
    internal void Update(JobProgress progress) =>
        _entry?.Publish(progress.SuccessfulSteps + progress.FailedSteps, progress.TotalSteps);

    /// <summary>
    /// Removes the job from the gauges.
    /// </summary>
    internal void Release()
    {
        _entry?.Release();
        _entry = null;
    }
}
