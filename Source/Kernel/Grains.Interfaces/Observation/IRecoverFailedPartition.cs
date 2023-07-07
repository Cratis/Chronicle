// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Defines a job that will attempt to recover a failed partition for an observer.
/// </summary>
public interface IRecoverFailedPartition : IGrainWithGuidCompoundKey
{
    /// <summary>
    /// Initiates recovery of a failed partition on an observer.
    /// </summary>
    /// <param name="observerKey">Key for the observer that is the source of this failed partition.</param>
    /// <param name="observerName">The name of the observer that has failed partition.</param>
    /// <param name="fromEvent">The event to start recovering from.</param>
    /// <param name="eventTypes">Event types to filter.</param>
    /// <param name="messages">Any messages associated with the failure.</param>
    /// <param name="stackTrace">A stack trace associated with the failure.</param>
    /// /// <returns>Awaitable task.</returns>
    Task Recover(ObserverKey observerKey, ObserverName observerName, EventSequenceNumber fromEvent, IEnumerable<EventType> eventTypes, IEnumerable<string> messages, string stackTrace);

    /// <summary>
    /// Catches up any additional events on a failed partition after recovery was completed.
    /// </summary>
    /// <param name="fromEvent">The event to start catching up from.</param>
    /// <returns>Awaitable task.</returns>
    Task Catchup(EventSequenceNumber fromEvent);

    /// <summary>
    /// Resets the recovery state for a failed partition.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Reset();
}
