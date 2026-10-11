// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_child_joins;

public class with_a_keyed_record_child : given.a_child_join_builder
{
    ChildrenDefinition _children;

    void Because() => _children = _builder.Build(typeof(Order)).Children["lines"];

    [Fact] void should_join_on_the_child_identity() => _children.Join.Values.Single().On.ShouldEqual(_children.IdentifiedBy);
    [Fact] void should_normalize_the_child_identity() => _children.Join.Values.Single().On.ShouldEqual("productId");
    [Fact] void should_map_the_joined_property() => _children.Join.Values.Single().Properties["productName"].ShouldEqual("name");
}
