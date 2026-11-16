// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionEventContext;

public class when_adding_futures_for_pinned_generations : given.a_context
{
    void Establish() => _context = _context with { GenerationDelivery = EventGenerationDelivery.Pinned };

    void Because()
    {
        _context.AddDeferredFuture(FutureFor(new EventType("event", 1)));
        _context.AddDeferredFuture(FutureFor(new EventType("event", 1)));
        _context.AddDeferredFuture(FutureFor(new EventType("event", 2)));
    }

    [Fact] void should_retain_each_distinct_generation() => _context.DeferredFutures.Select(_ => _.Event.Context.EventType.Generation).ShouldEqual([1, 2]);
}
