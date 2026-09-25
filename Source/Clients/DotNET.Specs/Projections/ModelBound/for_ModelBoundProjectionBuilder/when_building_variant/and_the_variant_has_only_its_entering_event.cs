// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_variant;

public class and_the_variant_has_only_its_entering_event : given.a_model_bound_projection_builder_for_variants
{
    ProjectionDefinition _result;

    void Because() => _result = builder.BuildVariant(typeof(BacklogItem), [typeof(DevelopmentItem), typeof(PullRequestItem)]);

    [Fact]
    void should_have_a_from_definition_for_the_entering_event()
    {
        var eventType = event_types.GetEventTypeFor(typeof(IssueCreated)).ToContract();
        _result.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_be_removed_with_development_items_entering_event()
    {
        var eventType = event_types.GetEventTypeFor(typeof(IssueStarted)).ToContract();
        _result.RemovedWith.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_be_removed_with_pull_request_items_entering_event()
    {
        var eventType = event_types.GetEventTypeFor(typeof(PullRequestCreated)).ToContract();
        _result.RemovedWith.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_not_be_removed_with_its_own_entering_event()
    {
        var eventType = event_types.GetEventTypeFor(typeof(IssueCreated)).ToContract();
        _result.RemovedWith.Keys.ShouldNotContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_have_no_join_definitions() => _result.Join.ShouldBeEmpty();
}
