// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_a_root_join_declares_a_context_key : given.a_root_join
{
    protected override PropertyExpression JoinKey => "$eventContext.Subject";
    protected override string JoinSource => JoinValue;

    async Task Because() => await ApplyJoin();

    [Fact] void should_keep_matching_by_the_event_source() => _resolvedKey.Key.Value.ShouldEqual(_rootId);
    [Fact] void should_update_the_root() => Root["name"].ShouldEqual("after");
    [Fact] void should_warn_once_when_building_the_projection() => WarningCount.ShouldEqual(1);
}
