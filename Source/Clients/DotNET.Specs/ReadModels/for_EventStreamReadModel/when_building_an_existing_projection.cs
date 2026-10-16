// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.ReadModels.for_EventStreamReadModel;

public class when_building_an_existing_projection : given.stream_models
{
    ProjectionDefinition _modelBoundDefinition;
    ProjectionDefinition _fluentDefinition;

    void Because()
    {
        _modelBoundDefinition = _modelBound.Build(typeof(ExistingModel));
        var builder = Fluent<ExistingModel>();
        builder.From<MoneyWithdrawn>();
        _fluentDefinition = builder.Build();
    }

    [Fact] void should_keep_model_bound_materialization() => _modelBoundDefinition.IsActive.ShouldBeTrue();
    [Fact] void should_keep_fluent_materialization() => _fluentDefinition.IsActive.ShouldBeTrue();
    [Fact] void should_not_imply_passive_on_existing_models() => typeof(ExistingModel).IsPassive().ShouldBeFalse();
}
