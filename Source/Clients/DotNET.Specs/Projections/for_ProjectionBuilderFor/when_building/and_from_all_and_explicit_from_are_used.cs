// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class and_from_all_and_explicit_from_are_used : given.a_builder_with_an_event
{
    ProjectionDefinition _result;

    void Because()
    {
        builder.From<given.ProjectionEvent>(_ => _.Set(model => model.Name).To(@event => @event.Name));
        builder.FromAll(_ => _.Count(model => model.EventCount));
        _result = builder.Build();
    }

    [Fact] void should_subscribe_to_all_event_types() => _result.SubscribesToAllEvents.ShouldBeTrue();
    [Fact] void should_keep_the_explicit_from_mapping() => _result.From.Single().Value.Properties[nameof(ProjectionReadModel.Name)].ShouldEqual(nameof(given.ProjectionEvent.Name));
    [Fact] void should_keep_the_from_all_mapping() => _result.All.Properties[nameof(ProjectionReadModel.EventCount)].ShouldEqual(WellKnownExpressions.Count);
}
