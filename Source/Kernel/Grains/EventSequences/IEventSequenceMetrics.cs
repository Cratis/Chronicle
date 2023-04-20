// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Kernel.Grains.EventSequences;

/// <summary>
/// Defines a system that can track metrics for event sequences.
/// </summary>
public interface IEventSequenceMetrics
{
    void AppendedEvent(EventSourceId eventSourceId, string eventName);
    void DuplicateEventSequenceNumber(EventSourceId eventSourceId, string eventName);
    void FailedAppending(EventSourceId eventSourceId, string eventName);
}
