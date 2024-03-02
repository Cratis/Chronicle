// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation.Jobs;

/// <summary>
/// Represents the request for a <see cref="IReplayObserver"/>.
/// </summary>
/// <param name="ObserverId">The identifier of the observer to replay.</param>
/// <param name="ObserverKey">The additional <see cref="ObserverKey"/> for the observer to replay.</param>
/// <param name="ObserverSubscription">The <see cref="ObserverSubscription"/> for the observer.</param>
/// <param name="EventTypes">The event types to replay.</param>
public record ReplayObserverRequest(
    ObserverId ObserverId,
    ObserverKey ObserverKey,
    ObserverSubscription ObserverSubscription,
    IEnumerable<EventType> EventTypes);
