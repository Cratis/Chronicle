// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Grains.Observation.States;

/// <summary>
/// Represents the evaluation context for replaying.
/// </summary>
/// <param name="Id">The <see cref="ObserverId"/> for the observer to evaluate.</param>
/// <param name="Key">The <see cref="ObserverKey"/> for the observer to evaluate.</param>
/// <param name="State">The <see cref="ObserverState"/> to evaluate.</param>
/// <param name="Subscription">The <see cref="ObserverSubscription"/> to evaluate against.</param>
/// <param name="TailEventSequenceNumber">The tail <see cref="EventSequenceNumber"/> of the event sequence.</param>
/// <param name="TailEventSequenceNumberForEventTypes">The tail <see cref="EventSequenceNumber"/> for the event types in the event sequence.</param>
public record ReplayEvaluationContext(
    ObserverId Id,
    ObserverKey Key,
    ObserverState State,
    ObserverSubscription Subscription,
    EventSequenceNumber TailEventSequenceNumber,
    EventSequenceNumber TailEventSequenceNumberForEventTypes);
