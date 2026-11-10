// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents a running observer job in the <see cref="IObserverJobGauges"/>.
/// </summary>
/// <remarks>
/// The entry holds only the job's identity and its last published progress, never the job activation.
/// </remarks>
public sealed class ObserverJobGaugeEntry
{
    readonly Action<ObserverJobGaugeEntry> _release;
    ObserverJobGaugeProgress _progress = ObserverJobGaugeProgress.None;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverJobGaugeEntry"/> class.
    /// </summary>
    /// <param name="jobId">The <see cref="JobId"/> of the job.</param>
    /// <param name="observerKey">The <see cref="ObserverKey"/> of the observer the job works for.</param>
    /// <param name="jobType">The <see cref="ObserverJobGaugeType"/> of the job.</param>
    /// <param name="release">Removes the entry from its registry.</param>
    internal ObserverJobGaugeEntry(JobId jobId, ObserverKey observerKey, ObserverJobGaugeType jobType, Action<ObserverJobGaugeEntry> release)
    {
        JobId = jobId;
        ObserverKey = observerKey;
        JobType = jobType;
        _release = release;
    }

    /// <summary>
    /// Gets the <see cref="JobId"/> of the job.
    /// </summary>
    public JobId JobId { get; }

    /// <summary>
    /// Gets the <see cref="ObserverKey"/> of the observer the job works for.
    /// </summary>
    public ObserverKey ObserverKey { get; }

    /// <summary>
    /// Gets the <see cref="ObserverJobGaugeType"/> of the job.
    /// </summary>
    public ObserverJobGaugeType JobType { get; }

    /// <summary>
    /// Gets the last published progress.
    /// </summary>
    public ObserverJobGaugeProgress Progress => Volatile.Read(ref _progress);

    /// <summary>
    /// Publishes the progress of the job.
    /// </summary>
    /// <param name="handledSteps">The number of steps that completed, successfully or with failures.</param>
    /// <param name="totalSteps">The number of steps of the job.</param>
    public void Publish(int handledSteps, int totalSteps) =>
        Volatile.Write(ref _progress, new(Math.Max(handledSteps, 0), Math.Max(totalSteps, 0)));

    /// <summary>
    /// Removes the job from the gauges.
    /// </summary>
    public void Release() => _release(this);
}
