// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Testing.Reactors;

public class ConcurrencyScopedReservationReactor : IReactor
{
    public EventsWithConcurrencyScopes Handle(ReservationMade @event, EventContext context) =>
        new(
            [new(context.EventSourceId, new MemberActivityRecorded())],
            [new(context.EventSourceId, ConcurrencyScope.NotSet)]);
}
