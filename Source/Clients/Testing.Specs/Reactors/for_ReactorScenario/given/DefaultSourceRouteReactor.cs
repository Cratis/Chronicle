// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

/// <summary>
/// Records only events with the literal default source type.
/// </summary>
/// <param name="recorder">The destination for observed input.</param>
[EventSourceType("Default")]
public class DefaultSourceRouteReactor(IRecordEventRoute recorder) : IReactor
{
    /// <summary>
    /// Records a matching event's route.
    /// </summary>
    /// <param name="event">The delivered event.</param>
    /// <param name="context">The delivered context.</param>
    public void Handle(ReactorRouteRecorded @event, EventContext context) => recorder.Record(@event, context);
}
