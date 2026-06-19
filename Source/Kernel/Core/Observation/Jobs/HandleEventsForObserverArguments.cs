// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents the arguments passed along to a job step that handles events for an observer in event-sequence order.
/// </summary>
/// <param name="ObserverKey">The <see cref="ObserverKey"/> with extended details about the observer.</param>
/// <param name="ObserverType">The <see cref="ObserverType"/>.</param>
/// <param name="StartEventSequenceNumber">The event sequence number the job step should start from.</param>
/// <param name="EndEventSequenceNumber">The event sequence number the job step should go to.</param>
/// <param name="EventObservationState">The event observation state to set for the events.</param>
/// <param name="EventTypes">The event types that are to replay.</param>
public record HandleEventsForObserverArguments(
    ObserverKey ObserverKey,
    ObserverType ObserverType,
    EventSequenceNumber StartEventSequenceNumber,
    EventSequenceNumber EndEventSequenceNumber,
    EventObservationState EventObservationState,
    IEnumerable<EventType> EventTypes) : IObserverJobRequest;

