// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable
#pragma warning disable CA1819 // Allow arrays on properties

namespace Aksio.Cratis.Events.Store.Observation;

/// <summary>
/// Represents the state used for failed observers.
/// </summary>
public class FailedObserverState
{
    /// <summary>
    /// The name of the storage provider used for working with this type of state.
    /// </summary>
    public const string StorageProvider = "failed-observer-state-provider";

    string _id;

    /// <summary>
    /// Gets the unique identifier for the state.
    /// </summary>
    public string Id
    {
        get => _id;
        set
        {
            _id = value;
            var (eventSequenceId, observerId, eventSourceId) = Parse(value);
            EventSequenceId = eventSequenceId;
            ObserverId = observerId;
            EventSourceId = eventSourceId;
        }
    }

    /// <summary>
    /// Gets the event log identifier.
    /// </summary>
    public EventSequenceId EventSequenceId { get; private set; }

    /// <summary>
    /// Gets the observer identifier.
    /// </summary>
    public ObserverId ObserverId { get; private set; }

    /// <summary>
    /// Gets the event source identifier.
    /// </summary>
    public EventSourceId EventSourceId { get; private set; }

    /// <summary>
    /// Gets or sets whether or not the partition has failed.
    /// </summary>
    public bool IsFailed { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="EventSequenceNumber"/> of the failure - if any.
    /// </summary>
    public EventSequenceNumber SequenceNumber { get; set; } = EventSequenceNumber.First;

    /// <summary>
    /// Gets or sets the event types for the observer.
    /// </summary>
    public IEnumerable<EventType> EventTypes { get; set; } = Array.Empty<EventType>();

    /// <summary>
    /// Gets or sets the occurred time of the failure - if any.
    /// </summary>
    public DateTimeOffset Occurred { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the number of retry attempts it has had.
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>
    /// Gets or sets the message from the failure - if any.
    /// </summary>
    public string[] Messages { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the stack trace from the failure - if any.
    /// </summary>
    public string StackTrace { get; set; } = string.Empty;

    /// <summary>
    /// Create a composite key.
    /// </summary>
    /// <param name="eventSequenceId">The Event sequence.</param>
    /// <param name="observerId">The observer.</param>
    /// <param name="eventSourceId">The event source.</param>
    /// <returns>The composite key.</returns>
    public static string CreateKeyFrom(EventSequenceId eventSequenceId, ObserverId observerId, EventSourceId eventSourceId) => $"{eventSequenceId}+{observerId}+{eventSourceId}";

    /// <summary>
    /// Parse a string representation of a composite key.
    /// </summary>
    /// <param name="key">Key to parse.</param>
    /// <returns>Tuple holding event log, observer and event source.</returns>
    public static (EventSequenceId EventSequenceId, ObserverId ObserverId, EventSourceId EventSourceId) Parse(string key)
    {
        var segments = key.Split('+');
        return (
            segments[0],
            segments[1],
            segments[2]
        );
    }
}
