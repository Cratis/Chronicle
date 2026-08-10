// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class and_child_disables_automap : Specification
{
    ProjectionDefinition _result;

    void Because()
    {
        var builder = new ProjectionBuilderFor<AutoMapParentReadModel>(
            new ProjectionId(typeof(AutoMapParentReadModel).FullName),
            typeof(AutoMapParentReadModel),
            new DefaultNamingPolicy(),
            new EventTypesForSpecifications([]),
            new JsonSerializerOptions());
        builder.Children(_ => _.Children, child => child.NoAutoMap());
        _result = builder.Build();
    }

    [Fact] void should_serialize_disabled() => _result.Children[nameof(AutoMapParentReadModel.Children)].AutoMap.ShouldEqual(Contracts.Projections.AutoMap.Disabled);
}
