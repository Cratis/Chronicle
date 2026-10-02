// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_variant;

public class and_a_from_event_overlaps_a_join_with_default_on : given.a_model_bound_projection_builder_for_variants
{
    ProjectionDefinition _result;

    void Because() => _result = builder.BuildVariant(typeof(VariantWithDefaultJoin), []);

    [Fact] void should_keep_only_the_entering_event_as_from() => _result.From.Keys.Select(_ => _.ToClient()).ShouldContainOnly(typeof(IssueCreated).GetEventType());
    [Fact] void should_have_one_value_keyed_join() => _result.Join.ToDictionary(_ => _.Key.ToClient(), _ => _.Value).Keys.ShouldContainOnly(typeof(VariantJoinedEvent).GetEventType());
    [Fact] void should_join_on_the_annotated_member_not_the_variant_key() => _result.Join.Single().Value.On.ShouldEqual(nameof(VariantWithDefaultJoin.Joined));
    [Fact] void should_keep_the_join_key_instead_of_the_from_key() => _result.Join.Single().Value.Key.ShouldEqual(WellKnownExpressions.EventSourceId);
    [Fact] void should_preserve_the_disjoint_from_mapping() => _result.Join.Single().Value.Properties[nameof(VariantWithDefaultJoin.FromOnly)].ShouldEqual(nameof(VariantJoinedEvent.FromValue));
    [Fact] void should_prefer_the_join_mapping_for_the_conflicting_property() => _result.Join.Single().Value.Properties[nameof(VariantWithDefaultJoin.Joined)].ShouldEqual(nameof(VariantJoinedEvent.JoinValue));

    [VariantOf<WorkItem>]
    [EntersOn<IssueCreated>]
    [FromEvent<VariantJoinedEvent>(key: nameof(VariantJoinedEvent.FromValue))]
    public record VariantWithDefaultJoin(
        [property: Key] Guid Id,
        [property: SetFrom<VariantJoinedEvent>(nameof(VariantJoinedEvent.FromValue))] string FromOnly,
        [property: SetFrom<VariantJoinedEvent>(nameof(VariantJoinedEvent.FromValue)), Join<VariantJoinedEvent>(eventPropertyName: nameof(VariantJoinedEvent.JoinValue))] string Joined);
}
