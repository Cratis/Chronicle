// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.ReadModels.for_EventStreamReadModel;

public class when_building_a_model_bound_stream_projection : given.stream_models
{
    ProjectionDefinition _definition;

    void Because() => _definition = _modelBound.Build(typeof(MonthlyWithdrawals));

    [Fact] void should_be_passive_without_an_explicit_passive_attribute() => _definition.IsActive.ShouldBeFalse();
    [Fact] void should_keep_the_source_key() => _definition.From.Values.Single().Key.ShouldEqual("$eventSourceId");
    [Fact] void should_be_discovered_as_passive() => typeof(MonthlyWithdrawals).IsPassive().ShouldBeTrue();
}
