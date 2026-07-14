// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines a system that selects which <see cref="ObserverSubscriberTarget"/> of a subscription a
/// partition's events are delivered to, fanning out across multiple connected client instances.
/// </summary>
public interface IObserverSubscriberSelector
{
    /// <summary>
    /// Selects the <see cref="ObserverSubscriberTarget"/> to deliver a partition's events to.
    /// </summary>
    /// <param name="subscription">The <see cref="ObserverSubscription"/> to select a target from.</param>
    /// <param name="partition">The <see cref="Key">partition</see> the events belong to.</param>
    /// <returns>The selected <see cref="ObserverSubscriberTarget"/>.</returns>
    ObserverSubscriberTarget Select(ObserverSubscription subscription, Key partition);
}
