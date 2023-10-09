// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Contracts.Observation;

/// <summary>
/// Represents the information about an observer.
/// </summary>
public class ObserverInformation
{
    /// <summary>
    /// Gets or sets the unique identifier of the observer.
    /// </summary>
    public Guid ObserverId { get; set; }

    /// <summary>
    /// Gets or sets the event sequence the observer is observing.
    /// </summary>
    public Guid EventSequenceId { get; set; }

    /// <summary>
    /// Gets or sets the name of the observer.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the type of observer.
    /// </summary>
    public ObserverType Type { get; set; }

    /// <summary>
    /// Gets or sets the types of events the observer is observing.
    /// </summary>
    public IEnumerable<EventType> EventTypes { get; set; }

    /// <summary>
    /// Gets or sets the next event sequence number the observer will observe.
    /// </summary>
    public ulong NextEventSequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the running state of the observer.
    /// </summary>
    public ObserverRunningState RunningState { get; set; }
}
