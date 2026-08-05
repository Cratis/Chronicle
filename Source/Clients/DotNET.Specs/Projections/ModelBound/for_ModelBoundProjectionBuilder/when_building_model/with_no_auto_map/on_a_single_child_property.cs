// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_no_auto_map;

/// <summary>
/// The per-property exclusion on a child, which used to compile, emit no diagnostic and do nothing.
/// </summary>
/// <remarks>
/// It had nowhere to travel: the children definition carried no exclusion list in the contract or on the wire,
/// so the kernel built every child projection with an empty set and a colliding event auto-mapped straight over
/// the value the author had sourced explicitly. The only placement that did work on a child was the class-level
/// one, which is blanket - one colliding property costs every other property on the child its auto-mapping.
/// </remarks>
public class on_a_single_child_property : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(LineAdded), typeof(LineTouched)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(BasketWithFencedChildProperty));

    ChildrenDefinition Children => _result.Children[nameof(BasketWithFencedChildProperty.Lines)];

    [Fact] void should_flag_the_marked_child_property_as_no_auto_map() => Children.NoAutoMapProperties.ShouldContain(nameof(BasketLine.Caption));
    [Fact] void should_not_flag_other_child_properties() => Children.NoAutoMapProperties.ShouldNotContain(nameof(BasketLine.Extra));
    [Fact] void should_keep_auto_map_enabled_for_the_child() => Children.AutoMap.ShouldEqual(Cratis.Chronicle.Contracts.Projections.AutoMap.Enabled);
    [Fact] void should_not_leak_the_child_exclusion_onto_the_root() => _result.NoAutoMapProperties.ShouldNotContain(nameof(BasketLine.Caption));
}

[EventType]
public record LineAdded(Guid LineId, string OriginalCaption, string Extra);

[EventType]
public record LineTouched(Guid LineId, string Caption);

public record BasketLine(
    Guid LineId,

    [SetFrom<LineAdded>(nameof(LineAdded.OriginalCaption))]
    [NoAutoMap]
    string Caption,

    string Extra);

[FromEvent<LineAdded>]
public record BasketWithFencedChildProperty(
    [Key] Guid Id,

    [ChildrenFrom<LineAdded>(key: nameof(LineAdded.LineId))]
    [ChildrenFrom<LineTouched>(key: nameof(LineTouched.LineId))]
    IReadOnlyList<BasketLine> Lines);
