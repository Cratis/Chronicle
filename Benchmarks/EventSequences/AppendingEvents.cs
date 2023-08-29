// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Identities;
using Aksio.Cratis.Kernel.Grains.EventSequences;
using Benchmark.Model;

namespace Benchmarks.EventSequences;

public class AppendingEvents : EventLogJob
{
    static readonly PersonId _personId = new(Guid.Parse("c7073ebc-87eb-48c8-b747-908fa0acdd17"));
    static readonly MaterialId _materialId = new(Guid.Parse("e2701406-2b28-4e9b-bc78-36a356682f13"));

    IEnumerable<EventToAppend> _eventsToAppend = Enumerable.Empty<EventToAppend>();

    [Params(10, 100, 1000)]
    public int EventsToCommit { get; set; } = 1000;

    protected override IEnumerable<Type> EventTypes => new[]
    {
        typeof(ItemAddedToCart),
        typeof(ItemRemovedFromCart),
        typeof(QuantityAdjustedForItemInCart),
    };

    [Benchmark]
    [InvocationCount(1)]
    public Task MultipleEventsInSequence() => Perform(async eventSequence =>
    {
        foreach (var @event in _eventsToAppend)
        {
            await eventSequence.Append(
                @event.EventSourceId,
                @event.EventType,
                @event.Content,
                GlobalVariables.BenchmarkCausation,
                Identity.System);
        }
    });

    [Benchmark]
    [InvocationCount(1)]
    public Task MultipleEventsInOneCall() => Perform(async eventSequence => await eventSequence.AppendMany(_eventsToAppend, GlobalVariables.BenchmarkCausation, Identity.System));

    protected override void Setup()
    {
        var eventTypes = EventTypes.ToArray();

        _eventsToAppend = Enumerable.Range(0, EventsToCommit).Select(index =>
        {
            var @event = GetEventInstanceFor(index);
            return new EventToAppend(
                _personId.Value,
                @event.GetType().GetEventType(),
                SerializeEvent(@event));
        }).ToArray();
    }

    object GetEventInstanceFor(int index)
    {
        switch (index % (EventTypes.Count() - 1))
        {
            case 0:
                return new ItemAddedToCart(_personId, _materialId, 1);
            case 1:
                return new ItemRemovedFromCart(_personId, _materialId);
        }

        return new QuantityAdjustedForItemInCart(_personId, _materialId, 1);
    }
}
