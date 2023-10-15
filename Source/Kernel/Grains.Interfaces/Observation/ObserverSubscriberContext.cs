// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Represents the context for an observer subscriber.
/// </summary>
/// <param name="ObservationState">The <see cref="EventObservationState"/> for the context.</param>
/// <param name="State">Optional state associated with the subscriber.</param>
public record ObserverSubscriberContext(EventObservationState ObservationState, object? State = null);
