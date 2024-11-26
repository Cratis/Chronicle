// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Storage.Jobs;
namespace Cratis.Chronicle.Grains.Observation.Jobs;

/// <summary>
/// Represents a <see cref="JobState"/> that knows about the last handled event.
/// </summary>
public class JobStateWithLastHandledEvent : JobState
{
    /// <summary>
    /// Gets or sets the event sequence number of the last handled event.
    /// </summary>
    public EventSequenceNumber LastHandledEventSequenceNumber { get; set; } = EventSequenceNumber.Unavailable;

    /// <summary>
    /// Handles state based on <see cref="JobStepResult"/>.
    /// </summary>
    /// <param name="result">The result.</param>
    public void HandleResult(JobStepResult result)
    {
        if (result.Result is HandleEventsForPartitionResult handleEventsResult)
        {
            LastHandledEventSequenceNumber = handleEventsResult.LastHandledEventSequenceNumber;
        }
    }
}
