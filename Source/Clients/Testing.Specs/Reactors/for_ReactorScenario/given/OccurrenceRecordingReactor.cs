// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

/// <summary>
/// Records the complete occurrence and delivery identity supplied to a handler.
/// </summary>
/// <param name="recorder">The argument recorder.</param>
public class OccurrenceRecordingReactor(IRecordOccurrence recorder) : IReactor
{
    /// <summary>
    /// Records the delivered occurrence.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The original context.</param>
    /// <param name="delivery">The delivery identity.</param>
    public void Handle(ReactorRouteRecorded @event, EventContext context, ReactorDelivery delivery) => recorder.Record(@event, context, delivery);
}
