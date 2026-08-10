// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class and_nested_has_no_auto_map_attributes : Specification
{
    ChildrenDefinition _result;

    void Because()
    {
        var builder = new ProjectionBuilderFor<AttributedNestedParentReadModel>(
            new ProjectionId(typeof(AttributedNestedParentReadModel).FullName),
            typeof(AttributedNestedParentReadModel),
            new DefaultNamingPolicy(),
            new EventTypesForSpecifications([]),
            new JsonSerializerOptions());
        builder.Nested(_ => _.Nested, _ => { });
        _result = builder.Build().Nested[nameof(AttributedNestedParentReadModel.Nested)];
    }

    [Fact] void should_disable_automap() => _result.AutoMap.ShouldEqual(Contracts.Projections.AutoMap.Disabled);
    [Fact] void should_serialize_the_nested_property_exclusion() => _result.NoAutoMapProperties.ShouldContain(nameof(AttributedNestedReadModel.FullName));
}

[NoAutoMap]
public record AttributedNestedReadModel([property: NoAutoMap] string FullName);
public record AttributedNestedParentReadModel(AttributedNestedReadModel? Nested);

#pragma warning restore SA1402 // File may only contain a single type
