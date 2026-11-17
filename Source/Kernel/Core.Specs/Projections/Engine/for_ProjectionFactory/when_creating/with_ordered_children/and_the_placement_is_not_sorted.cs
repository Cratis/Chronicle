// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating.with_ordered_children;

public class and_the_placement_is_not_sorted : given.a_projection_factory_for_definitions
{
    IProjection _result;

    async Task Because() => _result = await CreateWithOrderedChildren(new(ChildrenPlacement.Prepend, PropertyPath.NotSet));

    [Fact] void should_create_the_projection() => _result.ShouldNotBeNull();
}
