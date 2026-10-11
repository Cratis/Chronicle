// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionFutures;

public class when_adding_futures_for_different_generations : given.the_futures_grain
{
    int _pending;
    ProjectionEventContext _context;

    async Task Because()
    {
        var first = CreateFuture(1);
        var second = CreateFuture(1);
        second = second with { Event = second.Event with { Context = second.Event.Context with { EventType = new EventType("TestEvent", 2) } } };
        _context = ProjectionEventContext.Empty(new ObjectComparer(), first.Event) with { GenerationDelivery = EventGenerationDelivery.Pinned };
        _context.AddDeferredFuture(first);
        _context.AddDeferredFuture(second);
        await _grain.AddPinnedFuture(first);
        _pending = await _grain.AddPinnedFuture(second);
    }

    [Fact] void should_retain_both_generations_in_the_event_context() => _context.DeferredFutures.Count().ShouldEqual(2);
    [Fact] void should_persist_both_generations_of_the_physical_event() => _pending.ShouldEqual(2);
}
