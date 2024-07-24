// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation.Reducers;

namespace Cratis.Chronicle.Grains.Observation.Reducers.Clients;

/// <summary>
/// Defines a client reducer.
/// </summary>
public interface IReducer : IGrainWithStringKey
{
    /// <summary>
    /// Start the observer.
    /// </summary>
    /// <param name="eventTypes">The <see cref="EventType">event types</see> the observer is expecting.</param>
    /// <returns>Awaitable task.</returns>
    Task Start(IEnumerable<EventTypeWithKeyExpression> eventTypes);
}
