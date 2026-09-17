// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_variant;

/// <summary>
/// A shared handler is declared once for the identity and merged into every variant. Because the event it maps
/// is not the variant's entering event, it must land as an update-only join - a shared handler can bring an
/// active variant up to date, but must never create one.
/// </summary>
public class and_a_shared_handler_applies_to_the_variant : given.a_model_bound_projection_builder_for_variants
{
    ProjectionDefinition _result;

    void Because() => _result = builder.BuildVariant(typeof(BacklogItem), [typeof(PullRequestItem)], [typeof(WorkItemSharedHandlers)]);

    [Fact]
    void should_not_have_a_from_definition_for_the_shared_event()
    {
        var eventType = event_types.GetEventTypeFor(typeof(TitleChanged)).ToContract();
        _result.From.Keys.ShouldNotContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_have_a_join_definition_for_the_shared_event()
    {
        var eventType = event_types.GetEventTypeFor(typeof(TitleChanged)).ToContract();
        _result.Join.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_map_the_shared_property_on_the_join()
    {
        var eventType = event_types.GetEventTypeFor(typeof(TitleChanged)).ToContract();
        var join = _result.Join.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        join.Properties.Keys.ShouldContain(nameof(BacklogItem.Title));
    }

    [Fact]
    void should_join_on_the_variants_own_key_property()
    {
        var eventType = event_types.GetEventTypeFor(typeof(TitleChanged)).ToContract();
        var join = _result.Join.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        join.On.ShouldEqual(nameof(BacklogItem.Id));
    }

    [Fact]
    void should_still_have_a_from_definition_for_the_entering_event()
    {
        var eventType = event_types.GetEventTypeFor(typeof(IssueCreated)).ToContract();
        _result.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }
}
