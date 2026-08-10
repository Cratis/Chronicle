// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class and_child_inherits_automap : Specification
{
    ProjectionDefinition _result;

    void Because()
    {
        var builder = CreateBuilder();
        builder.Children(_ => _.Children, _ => { });
        _result = builder.Build();
    }

    [Fact] void should_serialize_inherit() => _result.Children[nameof(AutoMapParentReadModel.Children)].AutoMap.ShouldEqual(Contracts.Projections.AutoMap.Inherit);

    static ProjectionBuilderFor<AutoMapParentReadModel> CreateBuilder() =>
        new(
            new ProjectionId(typeof(AutoMapParentReadModel).FullName),
            typeof(AutoMapParentReadModel),
            new DefaultNamingPolicy(),
            new EventTypesForSpecifications([]),
            new JsonSerializerOptions());
}

public record AutoMapChildReadModel(string Id);
public record AutoMapParentReadModel(IEnumerable<AutoMapChildReadModel> Children);

#pragma warning restore SA1402 // File may only contain a single type
