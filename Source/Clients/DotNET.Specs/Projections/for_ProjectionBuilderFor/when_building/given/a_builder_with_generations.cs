// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building.given;

public class a_builder_with_generations : Specification
{
    protected ProjectionBuilderFor<Model> _builder;

    void Establish() => _builder = new("projection", typeof(Model), new DefaultNamingPolicy(), new EventTypesForSpecifications([typeof(First), typeof(Second), typeof(Other)]), new JsonSerializerOptions());

    [EventTypeGenerationFor<Second>(1)]
    public record First(string Name);

    [EventType("PinnedPerson", generation: 2)]
    public record Second(string Name);

    [EventType]
    public record Other(string Name);

    public record Model(string Name, IEnumerable<Child> Items, IEnumerable<Child> OtherItems, Child? Nested);

    public record Child(string Name);
}
