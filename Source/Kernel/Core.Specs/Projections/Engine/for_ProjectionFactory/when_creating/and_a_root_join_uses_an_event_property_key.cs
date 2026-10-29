// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_a_root_join_uses_an_event_property_key : given.a_root_join
{
    async Task Because() => await ApplyJoin();

    [Fact] void should_find_the_root_by_the_event_property() => _resolvedKey.Key.Value.ShouldEqual(_rootId);
    [Fact] void should_keep_the_guid_shaped_join_value_as_a_string() => _resolvedKey.JoinKey.ShouldEqual(JoinValue);
    [Fact] void should_update_the_root() => Root["name"].ShouldEqual("after");
    [Fact] void should_not_create_another_root() => _sink.Collection.Count.ShouldEqual(1);
    [Fact] void should_not_warn() => WarningCount.ShouldEqual(0);
}
