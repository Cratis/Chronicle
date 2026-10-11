// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionFutures;

public class when_adding_compatibility_futures_at_different_generations : given.the_futures_grain
{
    int _pending;
    ProjectionEventContext _context;

    async Task Because()
    {
        var first = CreateFuture(1);
        var second = CreateFuture(1);
        second = second with { Event = second.Event with { Context = second.Event.Context with { EventType = new EventType("TestEvent", 2) } } };
        _context = ProjectionEventContext.Empty(new ObjectComparer(), first.Event);
        _context.AddDeferredFuture(first);
        _context.AddDeferredFuture(second);
        await _grain.AddFuture(first);
        _pending = await _grain.AddFuture(second);
    }

    [Fact] void should_keep_existing_event_context_deduplication() => _context.DeferredFutures.Count().ShouldEqual(1);
    [Fact] void should_keep_existing_persisted_deduplication() => _pending.ShouldEqual(1);
}
