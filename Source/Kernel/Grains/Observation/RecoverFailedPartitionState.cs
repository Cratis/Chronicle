// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Holds the state for the job to recover a failed partition.
/// </summary>
public class RecoverFailedPartitionState
{
    /// <summary>
    /// Key for the storage provider for this state
    /// </summary>
    public const string StorageProvider = "recover-failed-partition";
    
    /// <summary>
    /// Unique identifier of the failed partition.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// Event log sequence number of the initial event that errored.
    /// </summary>
    public EventSequenceNumber InitialError { get; set; } = EventSequenceNumber.Unavailable;
    /// <summary>
    /// Event log sequence number of the last event that errored.
    /// </summary>
    public EventSequenceNumber CurrentError { get; set; } = EventSequenceNumber.Unavailable;
    /// <summary>
    /// Event log sequence number of the next event that should be processed.
    /// </summary>
    public EventSequenceNumber NextSequenceNumberToProcess { get; set; } = EventSequenceNumber.Unavailable;
    /// <summary>
    /// Total number of attempts to processed the failed partition since the initial error.
    /// </summary>
    public int NumberOfAttemptsOnSinceInitialised { get; set; }
    /// <summary>
    /// Number of attempts to processed the failed partition on the current error.
    /// </summary>
    public int NumberOfAttemptsOnCurrentError { get; set; }
    /// <summary>
    /// Date and time of the initial error of the failed partition in UTC.
    /// </summary>
    public DateTimeOffset InitialPartitionFailedOn { get; set; } = DateTimeOffset.MinValue;
    /// <summary>
    /// Date and time of the last attempt to process the failed partition in UTC.
    /// </summary>
    public DateTimeOffset? LastAttemptOnCurrentError { get; set; } = DateTimeOffset.MinValue;
    /// <summary>
    /// The event types that are being processed by the failed partition.
    /// </summary>
    public IEnumerable<EventType> EventTypes { get; set; } = Enumerable.Empty<EventType>();
    /// <summary>
    /// Gets or sets the <see cref="ObserverId"/> for which this is a failed partition.
    /// </summary>
    public ObserverId ObserverId { get; set; } = ObserverId.Unspecified;
    /// <summary>
    /// Gets or sets the StackTrace for the last error on this failed partition.
    /// </summary>
    public string StackTrace { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the Error Messages for the last error on this failed partition.
    /// </summary>
    public IEnumerable<string> Messages { get; set; } = Enumerable.Empty<string>();
    /// <summary>
    /// String form of the Observer Key for the origin of this failed partition
    /// </summary>
    public string ObserverKey { get; set; } = string.Empty;
    /// <summary>
    /// String form of the Subscriber key for the origin of this failed partition
    /// </summary>
    public string SubscriberKey { get; set; } = string.Empty;

    /// <summary>
    /// Resets the state of the failed partition.
    /// Metadata is not reset.
    /// </summary>
    public void Reset()
    {
        InitialError = EventSequenceNumber.Unavailable;
        CurrentError = EventSequenceNumber.Unavailable;
        NextSequenceNumberToProcess = EventSequenceNumber.Unavailable;
        NumberOfAttemptsOnSinceInitialised = 0;
        NumberOfAttemptsOnCurrentError = 0;
        InitialPartitionFailedOn = DateTimeOffset.MinValue;
        LastAttemptOnCurrentError = DateTimeOffset.MinValue;
        StackTrace = string.Empty;
        Messages = Enumerable.Empty<string>();
    }
    
    /// <summary>
    /// Updates the state with the latest error.
    /// </summary>
    /// <param name="latestError">Event Sequence Number for the latest error</param>
    /// <param name="messages">Error messages corresponding to the error</param>
    /// <param name="stacktrace">The stacktrace for the error</param>
    /// <param name="errored">When the error occurred</param>
    public void UpdateWithLatestError(EventSequenceNumber latestError, IEnumerable<string> messages, string stacktrace,
        DateTimeOffset errored)
    {
        if (CurrentError == latestError)
        {
            NumberOfAttemptsOnCurrentError++;
        }
        else
        {
            CurrentError = latestError;
            NumberOfAttemptsOnCurrentError = 1;
        }
        NumberOfAttemptsOnSinceInitialised++;
        LastAttemptOnCurrentError = errored;
        StackTrace = stacktrace;
        Messages = messages;
    }
    
    /// <summary>
    /// Updates the state with the latest succeeded event.
    /// </summary>
    /// <param name="processedEvent">Event that was successfully processed</param>
    public void UpdateWithLatestSuccess(AppendedEvent processedEvent)
    {
        NextSequenceNumberToProcess = processedEvent.Metadata.SequenceNumber + 1;
        if (processedEvent.Metadata.SequenceNumber != CurrentError) return;
        CurrentError = EventSequenceNumber.Unavailable;
        LastAttemptOnCurrentError = DateTimeOffset.MinValue;
        NumberOfAttemptsOnCurrentError = 0;
    }

    /// <summary>
    /// Returns when the next attempt should be made to process the failed partition.
    /// </summary>
    /// <returns>Timespan representing time to next attempt</returns>
    public TimeSpan GetNextAttemptSchedule()
    {
        if(CurrentError == InitialError && NumberOfAttemptsOnCurrentError == 0)
        {
            return TimeSpan.Zero;
        }

        // exponential backoff but with a max of 1 hour
        return TimeSpan.FromSeconds(Math.Min(60 * 60, Math.Pow(2, NumberOfAttemptsOnCurrentError)));
    }

    public void InitialiseError(EventSequenceNumber fromEvent, IEnumerable<EventType> eventTypes,
        ObserverKey observerKey, ObserverSubscriberKey subscriberKey)
    {
        CurrentError = fromEvent;
        InitialError = fromEvent;
        InitialPartitionFailedOn = DateTimeOffset.UtcNow;
        EventTypes = eventTypes;
        ObserverKey = observerKey.ToString();
        SubscriberKey = subscriberKey.ToString();
        NextSequenceNumberToProcess = fromEvent;
    }

    public bool HasBeenInitialised() => CurrentError != EventSequenceNumber.Unavailable;
}