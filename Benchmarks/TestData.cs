// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Dynamic;
using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Grains.EventSequences;
using Aksio.Cratis.Schemas;
using Benchmark.Model;
using Microsoft.Extensions.DependencyInjection;

namespace Benchmarks;

public static class TestData
{
    static readonly PersonId _personId = new(Guid.Parse("c7073ebc-87eb-48c8-b747-908fa0acdd17"));
    static readonly MaterialId _materialId = new(Guid.Parse("e2701406-2b28-4e9b-bc78-36a356682f13"));
    static IEventSerializer? _eventSerializer;

    public static IEnumerable<Type> EventTypes => new[]
    {
        typeof(ItemAddedToCart),
        typeof(ItemRemovedFromCart),
        typeof(QuantityAdjustedForItemInCart),
    };

    static IEventSerializer EventSerializer => _eventSerializer ??= GlobalVariables.ServiceProvider.GetRequiredService<IEventSerializer>()!;

    public static IEnumerable<EventToAppend> GenerateEventsToAppend(int count) =>
        Enumerable.Range(0, count).Select(index =>
        {
            var @event = GetEventInstanceFor(index);
            return new EventToAppend(
                _personId.Value,
                @event.GetType().GetEventType(),
                SerializeEvent(@event));
        }).ToArray();

    public static IEnumerable<AppendedEvent> GenerateAppendedEvents(int count) =>
        Enumerable.Range(0, count).Select(index =>
        {
            var @event = GetEventInstanceFor(index);
            var appendedEvent = AppendedEvent.EmptyWithEventType(@event.GetType().GetEventType());
            return appendedEvent with
            {
                Content = @event.AsExpandoObject(true),
                Metadata = appendedEvent.Metadata with
                {
                    SequenceNumber = (ulong)index
                }
            };
        }).ToArray();

    static object GetEventInstanceFor(int index)
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

    static JsonObject SerializeEvent(object @event) => EventSerializer.Serialize(@event).GetAwaiter().GetResult();
}
