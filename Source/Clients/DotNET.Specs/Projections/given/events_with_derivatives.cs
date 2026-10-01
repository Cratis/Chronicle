// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.given;

public class events_with_derivatives : Specification
{
    protected IEventTypes _eventTypes;

    void Establish() => _eventTypes = new EventTypesForSpecifications([typeof(ItemChanged), typeof(FirstItemChanged), typeof(SecondItemChanged), typeof(OtherEvent)]);

    public record Item(string Id, string Name, string LastEventSourceId);
    public record Container(IEnumerable<Item> Children, Item? Nested);

    public interface IItemChanged
    {
        string Name { get; }
    }

    [EventType]
    public record ItemChanged(string Id, string ParentId, string Name) : IItemChanged;

    [EventType]
    public record FirstItemChanged(string Id, string ParentId, string Name, string Description) : ItemChanged(Id, ParentId, Name);

    [EventType]
    public record SecondItemChanged(string Id, string ParentId, string Name) : ItemChanged(Id, ParentId, Name);

    [EventType]
    public record OtherEvent(string Name);
}
