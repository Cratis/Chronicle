// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.Observation.Reactors.Clients;

/// <summary>
/// Defines a client observer.
/// </summary>
public interface IReactor : IGrainWithStringKey
{
    /// <summary>
    /// Start the observer.
    /// </summary>
    /// <param name="eventTypes">The <see cref="EventType">event types</see> the observer is expecting.</param>
    /// <returns>Awaitable task.</returns>
    Task Start(IEnumerable<EventType> eventTypes);
}
