// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

/// <summary>
/// Records a reactor's input occurrence and delivery identity.
/// </summary>
public interface IRecordOccurrence
{
    /// <summary>
    /// Records the arguments received by the handler.
    /// </summary>
    /// <param name="event">The delivered event.</param>
    /// <param name="context">The event context.</param>
    /// <param name="delivery">The delivery identity.</param>
    void Record(ReactorRouteRecorded @event, EventContext context, ReactorDelivery delivery);
}
