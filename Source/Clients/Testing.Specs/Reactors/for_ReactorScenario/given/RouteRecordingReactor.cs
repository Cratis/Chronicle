// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

/// <summary>
/// Records every delivered route without observer filters.
/// </summary>
/// <param name="recorder">The destination for observed input.</param>
public class RouteRecordingReactor(IRecordEventRoute recorder) : IReactor
{
    /// <summary>
    /// Records the delivered route.
    /// </summary>
    /// <param name="event">The delivered event.</param>
    /// <param name="context">The delivered context.</param>
    public void Handle(ReactorRouteRecorded @event, EventContext context) => recorder.Record(@event, context);
}
