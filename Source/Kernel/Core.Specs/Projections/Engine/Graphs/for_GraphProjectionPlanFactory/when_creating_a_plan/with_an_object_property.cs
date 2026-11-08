// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Graphs.for_GraphProjectionPlanFactory.when_creating_a_plan;

public class with_an_object_property : given.a_plan_factory
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _factory.Create("organization", Nodes("Person", NodeFrom("PersonRegistered+1", ("address", "address"))), Graph));

    [Fact] void should_refuse_it() => _error.ShouldBeOfExactType<InvalidGraphProjection>();
}
