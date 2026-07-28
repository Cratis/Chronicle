// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueueRouter.given;

public class a_router : Specification
{
    protected AppendedEventsQueueRouter _router;

    protected virtual int QueueCount => 4;

    void Establish() => _router = new AppendedEventsQueueRouter(QueueCount);

    protected static ObserverKey ObserverKeyFor(string observerId) =>
        new(observerId, "Some event store", "Some namespace", "Some event sequence");
}
