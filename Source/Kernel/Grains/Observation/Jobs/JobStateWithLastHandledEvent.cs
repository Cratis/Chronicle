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
    /// Gets or sets the value indicating whether all the events were handled or not.
    /// </summary>
    public bool HandledAllEvents { get; set; }

    /// <summary>
    /// Handles state based on <see cref="JobStepResult"/>.
    /// </summary>
    /// <param name="result">The result.</param>
    public void HandleResult(JobStepResult result)
    {
        if (result.TryGetFullResult<HandleEventsForPartitionResult>(out var handleEventsResult, out _))
        {
            if (!LastHandledEventSequenceNumber.IsActualValue ||
                handleEventsResult.LastHandledEventSequenceNumber > LastHandledEventSequenceNumber)
            {
                LastHandledEventSequenceNumber = handleEventsResult.LastHandledEventSequenceNumber;
                HandledAllEvents = true;
            }
        }
        else if (handleEventsResult is not null)
        {
            if (!LastHandledEventSequenceNumber.IsActualValue ||
                handleEventsResult.LastHandledEventSequenceNumber > LastHandledEventSequenceNumber)
            {
                LastHandledEventSequenceNumber = handleEventsResult.LastHandledEventSequenceNumber;
            }
        }
    }
}
