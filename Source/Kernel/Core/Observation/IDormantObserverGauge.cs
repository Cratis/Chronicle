// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines the gauge of observers held as dormant by the appended events queues of a silo (decision 0034).
/// </summary>
public interface IDormantObserverGauge
{
    /// <summary>
    /// Registers an appended events queue that holds dormant observers.
    /// </summary>
    /// <param name="eventSequenceKey">The <see cref="EventSequenceKey"/> of the queue's event sequence.</param>
    /// <param name="count">Gets how many observers the queue holds as dormant right now.</param>
    /// <returns>A handle that takes the queue out of the gauge once disposed, when the queue is deactivated.</returns>
    IDisposable Register(EventSequenceKey eventSequenceKey, Func<int> count);
}
