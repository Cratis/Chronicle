// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ChildrenBuilder.when_ordering.given;

public class a_children_builder : Specification
{
    public record Entry(string Id, string Name, DateTimeOffset Added);
    public record Container(IEnumerable<Entry> Entries);

    [EventType]
    public record EntryAdded(string Id, string Name);

    protected ChildrenBuilder<Container, Entry> _builder;
    protected ChildrenDefinition _result;

    protected virtual INamingPolicy NamingPolicy => new DefaultNamingPolicy();

    void Establish()
    {
        _builder = new(NamingPolicy, new EventTypesForSpecifications([typeof(EntryAdded)]), new JsonSerializerOptions(), AutoMap.Inherit);
        _builder.IdentifiedBy(_ => _.Id);
        _builder.From<EntryAdded>(from => from.UsingKey(_ => _.Id));
    }
}
