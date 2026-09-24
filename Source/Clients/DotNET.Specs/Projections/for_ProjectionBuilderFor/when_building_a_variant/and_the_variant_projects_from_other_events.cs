// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building_a_variant;

/// <summary>
/// The fluent path must express the same semantics as the model-bound one: only the event named with
/// <c language="csharp">EntersOn</c> keeps its create-or-update handler, and everything else the variant
/// projects from becomes an update-only join on the variant's own key.
/// </summary>
public class and_the_variant_projects_from_other_events : Specification
{
    ProjectionBuilderFor<PullRequestItem> _builder;
    IEventTypes _eventTypes;
    ProjectionDefinition _result;

    void Establish()
    {
        _eventTypes = new EventTypesForSpecifications([typeof(IssueCreated), typeof(PullRequestCreated), typeof(BuildCompleted)]);
        _builder = new ProjectionBuilderFor<PullRequestItem>(
            new ProjectionId(typeof(PullRequestItem).FullName),
            typeof(PullRequestItem),
            new DefaultNamingPolicy(),
            _eventTypes,
            new JsonSerializerOptions());
    }

    void Because()
    {
        _builder
            .VariantOf<WorkItem>(_ => _.Id)
            .EntersOn<PullRequestCreated>()
            .From<BuildCompleted>();
        _result = _builder.Build();
    }

    [Fact]
    void should_have_a_from_definition_for_the_entering_event()
    {
        var eventType = _eventTypes.GetEventTypeFor(typeof(PullRequestCreated)).ToContract();
        _result.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_not_have_a_from_definition_for_the_other_event()
    {
        var eventType = _eventTypes.GetEventTypeFor(typeof(BuildCompleted)).ToContract();
        _result.From.Keys.ShouldNotContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_have_a_join_definition_for_the_other_event()
    {
        var eventType = _eventTypes.GetEventTypeFor(typeof(BuildCompleted)).ToContract();
        _result.Join.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_join_on_the_key_the_variant_declared()
    {
        var eventType = _eventTypes.GetEventTypeFor(typeof(BuildCompleted)).ToContract();
        _result.Join.Single(kvp => kvp.Key.IsEqual(eventType)).Value.On.ShouldEqual(nameof(PullRequestItem.Id));
    }

    [Fact]
    void should_declare_itself_a_variant_of_the_identity() => _builder.VariantDeclaration!.Identity.ShouldEqual(typeof(WorkItem));
}
