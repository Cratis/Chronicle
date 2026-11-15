// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Defines the process-wide registry the observer job gauges are sampled from.
/// </summary>
/// <remarks>
/// Each process reports only the jobs it hosts. A job registers when it starts or resumes its steps, publishes its
/// progress as steps complete, and releases its entry when it completes, stops or deactivates.
/// </remarks>
public interface IObserverJobGauges
{
    /// <summary>
    /// Registers a running observer job.
    /// </summary>
    /// <param name="jobId">The <see cref="JobId"/> of the job.</param>
    /// <param name="observerKey">The <see cref="ObserverKey"/> of the observer the job works for.</param>
    /// <param name="jobType">The <see cref="ObserverJobGaugeType"/> of the job.</param>
    /// <returns>The entry to publish progress to; releasing it removes the job from the gauges.</returns>
    ObserverJobGaugeEntry Register(JobId jobId, ObserverKey observerKey, ObserverJobGaugeType jobType);
}
