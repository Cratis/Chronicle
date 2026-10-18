// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class and_from_all_sets_a_constant : given.a_projection_builder
{
    ProjectionDefinition _result;

    void Because()
    {
        builder.FromAll(_ => _.Set(model => model.Name).ToValue("constant"));
        _result = builder.Build();
    }

    [Fact] void should_subscribe_to_all_event_types() => _result.SubscribesToAllEvents.ShouldBeTrue();
    [Fact] void should_keep_the_constant_mapping() => _result.All.Properties[nameof(ProjectionReadModel.Name)].ShouldEqual("$value(constant)");
}
