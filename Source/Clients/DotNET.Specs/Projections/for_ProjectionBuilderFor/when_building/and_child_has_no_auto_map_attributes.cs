// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class and_child_has_no_auto_map_attributes : Specification
{
    ChildrenDefinition _result;

    void Because()
    {
        var builder = new ProjectionBuilderFor<AttributedChildParentReadModel>(
            new ProjectionId(typeof(AttributedChildParentReadModel).FullName),
            typeof(AttributedChildParentReadModel),
            new DefaultNamingPolicy(),
            new EventTypesForSpecifications([]),
            new JsonSerializerOptions());
        builder.Children(_ => _.Children, _ => { });
        _result = builder.Build().Children[nameof(AttributedChildParentReadModel.Children)];
    }

    [Fact] void should_disable_automap() => _result.AutoMap.ShouldEqual(Contracts.Projections.AutoMap.Disabled);
    [Fact] void should_serialize_the_child_property_exclusion() => _result.NoAutoMapProperties.ShouldContain(nameof(AttributedChildReadModel.FullName));
}

[NoAutoMap]
public record AttributedChildReadModel(string Id, [property: NoAutoMap] string FullName);
public record AttributedChildParentReadModel(IEnumerable<AttributedChildReadModel> Children);

#pragma warning restore SA1402 // File may only contain a single type
