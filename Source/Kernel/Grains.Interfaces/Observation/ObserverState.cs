// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Represents the state used for an observer.
/// </summary>
public class ObserverState
{
    EventSequenceNumber _nextEventSequenceNumber = EventSequenceNumber.First;

    /// <summary>
    /// Gets or sets the identifier of the observer state.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event types the observer is observing.
    /// </summary>
    public IEnumerable<EventType> EventTypes { get; set; } = Array.Empty<EventType>();

    /// <summary>
    /// Gets or sets the <see cref="EventSequenceId"/> the state is for.
    /// </summary>
    public EventSequenceId EventSequenceId { get; set; } = EventSequenceId.Unspecified;

    /// <summary>
    /// Gets or sets the <see cref="EventSequenceId"/> the state is for.
    /// </summary>
    public ObserverId ObserverId { get; set; } = ObserverId.Unspecified;

    /// <summary>
    /// Gets or sets a friendly name for the observer.
    /// </summary>
    public ObserverName Name { get; set; } = ObserverName.NotSpecified;

    /// <summary>
    /// Gets or sets the <see cref="ObserverType"/>.
    /// </summary>
    public ObserverType Type { get; set; } = ObserverType.Unknown;

    /// <summary>
    /// Gets or sets the expected next event sequence number into the event log.
    /// </summary>
    /// <remarks>
    /// When setting a value that is not an actual value, such as the system well known values, it will be set to <see cref="EventSequenceNumber.First"/>.
    /// We only want to see actual values here.
    /// </remarks>
    public EventSequenceNumber NextEventSequenceNumber
    {
        get => _nextEventSequenceNumber;
        set => _nextEventSequenceNumber = !value.IsActualValue ? EventSequenceNumber.First : value;
    }

    /// <summary>
    /// Gets or sets the last handled event in the event log, ever. This value will never reset during a rewind.
    /// </summary>
    public EventSequenceNumber LastHandled { get; set; } = EventSequenceNumber.First;

    /// <summary>
    /// Gets or sets the running state.
    /// </summary>
    public ObserverRunningState RunningState { get; set; } = ObserverRunningState.New;
}
