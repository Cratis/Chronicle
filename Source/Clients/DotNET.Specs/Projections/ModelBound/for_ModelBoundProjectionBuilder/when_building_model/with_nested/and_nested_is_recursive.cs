// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_nested;

public class and_nested_is_recursive : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([
            typeof(OuterNestedSet),
            typeof(InnerNestedSet)
        ]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(OuterModelWithNestedNested));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();
    [Fact] void should_have_one_top_level_nested_entry() => _result.Nested.Count.ShouldEqual(1);
    [Fact] void should_have_nested_entry_for_outer() => _result.Nested.Keys.ShouldContain(nameof(OuterModelWithNestedNested.Outer));

    [Fact]
    void should_have_inner_nested_entry_within_outer()
    {
        var outerDef = _result.Nested[nameof(OuterModelWithNestedNested.Outer)];
        outerDef.Nested.Keys.ShouldContain(nameof(OuterNestedItem.Inner));
    }

    [Fact]
    void should_have_from_definition_for_inner_nested_set()
    {
        var eventType = event_types.GetEventTypeFor(typeof(InnerNestedSet)).ToContract();
        var outerDef = _result.Nested[nameof(OuterModelWithNestedNested.Outer)];
        var innerDef = outerDef.Nested[nameof(OuterNestedItem.Inner)];
        innerDef.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }
}

[EventType]
public record OuterNestedSet(string OuterName);

[EventType]
public record InnerNestedSet(string InnerValue);

[FromEvent<InnerNestedSet>]
public record InnerNestedItem(string InnerValue);

[FromEvent<OuterNestedSet>]
public record OuterNestedItem(
    string OuterName,
    [Nested] InnerNestedItem? Inner);

public record OuterModelWithNestedNested(
    string RootName,
    [Nested] OuterNestedItem? Outer);
