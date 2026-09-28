// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projections.when_registering_explicitly;

public class a_read_model_without_model_bound_attributes : given.projections_for_explicit_registration
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(_projections.Register<NotModelBound>);

    [Fact] void should_fail() => _error.ShouldBeOfExactType<ReadModelIsNotModelBound>();
    [Fact] void should_not_know_the_read_model() => _projections.HasFor<NotModelBound>().ShouldBeFalse();
}
