// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Storage.Jobs;

namespace Cratis.Chronicle.Observation.Jobs;

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
    /// Gets or sets the value indicating whether the step completed without leaving events behind.
    /// </summary>
    /// <remarks>
    /// This is true both when every event was handled and when the step succeeded having read no events at
    /// all — on its own it says nothing about whether any work was done. Use
    /// <see cref="SucceededWithoutHandlingAnyEvents"/> to tell the two apart.
    /// </remarks>
    public bool HandledAllEvents { get; set; }

    /// <summary>
    /// Gets a value indicating whether the step completed successfully without handling a single event.
    /// </summary>
    /// <remarks>
    /// "Successfully did nothing" is not "handled everything". Treating the two as the same lets a caller
    /// conclude work was done when the step never reached a subscriber.
    /// </remarks>
    public bool SucceededWithoutHandlingAnyEvents => HandledAllEvents && !LastHandledEventSequenceNumber.IsActualValue;

    /// <summary>
    /// The <see cref="ObserverKey"/>.
    /// </summary>
    public IObserverJobRequest ObserverRequest => (IObserverJobRequest)Request;

    /// <summary>
    /// The <see cref="ObserverDetails"/>.
    /// </summary>
    public ObserverDetails ObserverDetails => new(ObserverRequest.ObserverKey, ObserverRequest.ObserverType);

    /// <summary>
    /// Handles state based on <see cref="JobStepResult"/>.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="jsonSerializerOptions">The serializer options used to deserialize the result.</param>
    public void HandleResult(JobStepResult result, JsonSerializerOptions jsonSerializerOptions)
    {
        if (result.TryGetFullResult<HandleEventsForPartitionResult>(out var handleEventsResult, out _, jsonSerializerOptions))
        {
            if (IsNewerThanRecorded(handleEventsResult.LastHandledEventSequenceNumber))
            {
                LastHandledEventSequenceNumber = handleEventsResult.LastHandledEventSequenceNumber;
                HandledAllEvents = true;
            }
        }
        else if (handleEventsResult is not null)
        {
            if (IsNewerThanRecorded(handleEventsResult.LastHandledEventSequenceNumber))
            {
                LastHandledEventSequenceNumber = handleEventsResult.LastHandledEventSequenceNumber;
            }
        }
    }

    /// <summary>
    /// Check whether a sequence number reported by a step moves the recorded one forward.
    /// </summary>
    /// <param name="sequenceNumber">The <see cref="EventSequenceNumber"/> the step reported.</param>
    /// <returns>True when it should be recorded, false when the recorded one already says more.</returns>
    /// <remarks>
    /// <see cref="EventSequenceNumber.Unavailable"/> is the largest possible value, so a step that handled
    /// nothing would compare as newer than one that handled events and overwrite it. A step only moves the
    /// recorded number forward with an actual value.
    /// </remarks>
    bool IsNewerThanRecorded(EventSequenceNumber sequenceNumber)
    {
        if (!LastHandledEventSequenceNumber.IsActualValue)
        {
            return true;
        }

        return sequenceNumber.IsActualValue && sequenceNumber > LastHandledEventSequenceNumber;
    }
}
