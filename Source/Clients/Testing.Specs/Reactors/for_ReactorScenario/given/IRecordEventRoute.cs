// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

/// <summary>
/// Records the events and contexts delivered to a test reactor.
/// </summary>
public interface IRecordEventRoute
{
    /// <summary>
    /// Records a delivered event and its context.
    /// </summary>
    /// <param name="event">The delivered event.</param>
    /// <param name="context">The delivered context.</param>
    void Record(ReactorRouteRecorded @event, EventContext context);
}
