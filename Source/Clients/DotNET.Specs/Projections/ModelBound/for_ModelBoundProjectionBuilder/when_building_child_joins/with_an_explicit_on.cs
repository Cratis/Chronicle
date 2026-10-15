// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_child_joins;

public class with_an_explicit_on : given.a_child_join_builder
{
    ChildrenDefinition _children;

    void Because() => _children = _builder.Build(typeof(OrderWithExplicitOn)).Children["lines"];

    [Fact] void should_preserve_the_explicit_join_target() => _children.Join.Values.Single().On.ShouldEqual("otherProductId");
}
