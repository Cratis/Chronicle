// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projections.when_registering_explicitly;

public class a_second_projection_for_the_same_read_model : given.projections_for_explicit_registration
{
    IProjectionHandler _first;
    Exception _error;

    async Task Establish() => _first = await _projections.Register<Inventory>(projection => projection.From<ItemAdded>(), "first");

    async Task Because() => _error = await Catch.Exception(() => _projections.Register<Inventory>(projection => projection.From<ItemRemoved>(), "second"));

    [Fact] void should_fail() => _error.ShouldBeOfExactType<ReadModelAlreadyHasProjection>();
    [Fact] void should_keep_the_first_projection() => _projections.GetProjectionIdForModel<Inventory>().ShouldEqual(_first.Id);
    [Fact] void should_keep_a_single_definition() => _projections.Definitions.Count.ShouldEqual(1);
}
