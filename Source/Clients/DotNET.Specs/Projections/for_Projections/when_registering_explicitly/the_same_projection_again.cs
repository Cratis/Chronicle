// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projections.when_registering_explicitly;

public class the_same_projection_again : given.projections_for_explicit_registration
{
    async Task Establish() => await _projections.Register<Inventory>(projection => projection.From<ItemAdded>());

    async Task Because() => await _projections.Register<Inventory>(projection => projection.From<ItemRemoved>());

    [Fact] void should_keep_a_single_definition() => _projections.Definitions.Count.ShouldEqual(1);
    [Fact] void should_replace_the_definition() => _projections.Definitions.Single().From.Keys.Single().Id.ShouldEqual(nameof(ItemRemoved));
}
