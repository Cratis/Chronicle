// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Grains.Observation.Jobs;

/// <summary>
/// Represents the request for a <see cref="IReplayObserver"/>.
/// </summary>
/// <param name="ObserverKey">The additional <see cref="ObserverKey"/> for the observer to replay.</param>
/// <param name="ObserverType">The <see cref="ObserverType"/>.</param>
/// <param name="FromEventSequenceNumber">The <see cref="EventSequenceNumber"/> it should catch up from.</param>
/// <param name="EventTypes">The event types to replay.</param>
public record CatchUpObserverRequest(
    ObserverKey ObserverKey,
    ObserverType ObserverType,
    EventSequenceNumber FromEventSequenceNumber,
    IEnumerable<EventType> EventTypes) : IObserverJobRequest;
