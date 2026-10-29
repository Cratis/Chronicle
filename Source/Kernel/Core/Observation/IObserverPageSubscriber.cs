// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Orleans;
using Orleans.CodeGeneration;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines a subscriber that accepts a globally ordered page spanning partitions.
/// </summary>
[Version(1)]
public interface IObserverPageSubscriber : IObserverSubscriber
{
    /// <summary>
    /// Handles a page in sequence order, stopping at the first failing event.
    /// </summary>
    /// <param name="events">The ordered events to handle.</param>
    /// <param name="context">The subscriber context.</param>
    /// <returns>The handled position and, on failure, the failing partition and sequence number.</returns>
    [ResponseTimeout(ObserverSubscriberResponseTimeout.Value)]
    Task<ObserverSubscriberResult> OnNextPage(IEnumerable<AppendedEvent> events, ObserverSubscriberContext context);
}
