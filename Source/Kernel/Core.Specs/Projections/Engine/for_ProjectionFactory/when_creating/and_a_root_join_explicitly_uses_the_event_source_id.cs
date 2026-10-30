// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_a_root_join_explicitly_uses_the_event_source_id : given.a_root_join
{
    protected override PropertyExpression JoinKey => WellKnownExpressions.EventSourceId;
    protected override string JoinSource => JoinValue;

    async Task Because() => await ApplyJoin();

    [Fact] void should_find_the_root_by_the_event_source() => _resolvedKey.Key.Value.ShouldEqual(_rootId);
    [Fact] void should_update_the_root() => Root["name"].ShouldEqual("after");
    [Fact] void should_not_warn() => WarningCount.ShouldEqual(0);
}
