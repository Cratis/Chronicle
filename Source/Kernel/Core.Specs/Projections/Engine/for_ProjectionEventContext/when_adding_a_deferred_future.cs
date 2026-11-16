// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionEventContext;

public class when_adding_a_deferred_future : given.a_context
{
    void Because()
    {
        _context.AddDeferredFuture(FutureFor(new EventType("event", 1)));
        _context.AddDeferredFuture(FutureFor(new EventType("event", 1)));
        _context.AddDeferredFuture(FutureFor(new EventType("event", 2)));
    }

    [Fact] void should_keep_only_one_future_for_the_physical_event() => _context.DeferredFutures.Count().ShouldEqual(1);
    [Fact] void should_mark_the_context_deferred() => _context.IsDeferred.ShouldBeTrue();
}
