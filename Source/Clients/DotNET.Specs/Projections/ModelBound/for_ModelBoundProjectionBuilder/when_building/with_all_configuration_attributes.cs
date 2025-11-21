// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building;

public class with_all_configuration_attributes : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Because() => _result = builder.Build(typeof(ConfiguredProjection));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();
    [Fact] void should_use_custom_event_sequence() => _result.EventSequenceId.ShouldEqual("custom");
    [Fact] void should_not_be_active() => _result.IsActive.ShouldBeFalse();
    [Fact] void should_not_be_rewindable() => _result.IsRewindable.ShouldBeFalse();
}
