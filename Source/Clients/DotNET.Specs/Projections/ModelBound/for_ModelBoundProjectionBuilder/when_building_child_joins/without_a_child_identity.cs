// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_child_joins;

public class without_a_child_identity : given.a_child_join_builder
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _builder.Build(typeof(OrderWithoutChildIdentity)));

    [Fact] void should_reject_the_unresolvable_default_join_target() => _error.ShouldBeOfExactType<MissingChildIdentityForJoin>();
}
