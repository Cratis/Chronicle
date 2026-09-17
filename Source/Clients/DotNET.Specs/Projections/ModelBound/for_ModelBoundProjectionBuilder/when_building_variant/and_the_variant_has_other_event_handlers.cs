// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_variant;

/// <summary>
/// A variant can declare its own handlers for events other than the one that activates it (see
/// <see cref="PullRequestItem"/>'s <c language="csharp">BuildCompleted</c> handler). Because only the entering
/// event may create or resurrect the variant, every other handler must be reclassified from a create-or-update
/// <c language="csharp">From</c> into an update-only, self-referential <c language="csharp">Join</c>.
/// </summary>
public class and_the_variant_has_other_event_handlers : given.a_model_bound_projection_builder_for_variants
{
    ProjectionDefinition _result;

    void Because() => _result = builder.BuildVariant(typeof(PullRequestItem), [typeof(BacklogItem), typeof(DevelopmentItem)]);

    [Fact]
    void should_have_a_from_definition_for_the_entering_event()
    {
        var eventType = event_types.GetEventTypeFor(typeof(PullRequestCreated)).ToContract();
        _result.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_not_have_a_from_definition_for_build_completed()
    {
        var eventType = event_types.GetEventTypeFor(typeof(BuildCompleted)).ToContract();
        _result.From.Keys.ShouldNotContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_have_a_join_definition_for_build_completed()
    {
        var eventType = event_types.GetEventTypeFor(typeof(BuildCompleted)).ToContract();
        _result.Join.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_join_on_the_variants_own_key_property()
    {
        var eventType = event_types.GetEventTypeFor(typeof(BuildCompleted)).ToContract();
        var join = _result.Join.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        join.On.ShouldEqual(nameof(PullRequestItem.Id));
    }

    [Fact]
    void should_map_the_build_status_property_on_the_join()
    {
        var eventType = event_types.GetEventTypeFor(typeof(BuildCompleted)).ToContract();
        var join = _result.Join.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        join.Properties.Keys.ShouldContain(nameof(PullRequestItem.BuildStatus));
    }
}
