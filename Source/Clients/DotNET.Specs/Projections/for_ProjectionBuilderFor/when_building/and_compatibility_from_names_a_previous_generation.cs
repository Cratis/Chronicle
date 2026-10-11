// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class and_compatibility_from_names_a_previous_generation : given.a_builder_with_generations
{
    ProjectionDefinition _definition;

    void Establish() => _builder.From<First>();

    void Because() => _definition = _builder.Build();

    [Fact] void should_not_add_a_from_clause() => _definition.From.ShouldBeEmpty();
}
