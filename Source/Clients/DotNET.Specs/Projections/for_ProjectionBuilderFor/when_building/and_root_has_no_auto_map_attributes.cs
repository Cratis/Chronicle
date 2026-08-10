// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class and_root_has_no_auto_map_attributes : Specification
{
    ProjectionDefinition _result;

    void Because()
    {
        var builder = new ProjectionBuilderFor<NoAutoMapRootReadModel>(
            new ProjectionId(typeof(NoAutoMapRootReadModel).FullName),
            typeof(NoAutoMapRootReadModel),
            new DefaultNamingPolicy(),
            new EventTypesForSpecifications([]),
            new JsonSerializerOptions());
        _result = builder.Build();
    }

    [Fact] void should_disable_automap() => _result.AutoMap.ShouldEqual(Contracts.Projections.AutoMap.Disabled);
    [Fact] void should_serialize_the_root_property_exclusion() => _result.NoAutoMapProperties.ShouldContain(nameof(NoAutoMapRootReadModel.FullName));
}

[NoAutoMap]
public record NoAutoMapRootReadModel([property: NoAutoMap] string FullName);

#pragma warning restore SA1402 // File may only contain a single type
