// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_a_root_join_property_is_not_a_guid : given.a_root_join
{
    protected override string JoinValue => "organization-number";

    async Task Because() => await ApplyJoin();

    [Fact] void should_find_the_guid_keyed_root() => _resolvedKey.Key.Value.ShouldEqual(_rootId);
    [Fact] void should_keep_the_join_value_unconverted() => _resolvedKey.JoinKey.ShouldEqual(JoinValue);
    [Fact] void should_update_the_root() => Root["name"].ShouldEqual("after");
}
