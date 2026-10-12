// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_child_joins;

public class with_an_explicit_identity : given.a_child_join_builder
{
    ChildrenDefinition _children;

    void Because() => _children = _builder.Build(typeof(OrderWithExplicitIdentity)).Children["lines"];

    [Fact] void should_join_on_the_explicit_child_identity() => _children.Join.Values.Single().On.ShouldEqual("itemId");
}
