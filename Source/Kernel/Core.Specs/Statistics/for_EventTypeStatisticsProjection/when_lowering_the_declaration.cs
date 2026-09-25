// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Kernel;

namespace Cratis.Chronicle.Statistics.for_EventTypeStatisticsProjection;

/// <summary>
/// The figures the dashboard and the event store cards read are folds over this projection's rows, so what it is
/// keyed by and what it subscribes to is the whole of whether those figures are right. Pinned here rather than
/// left to be discovered from a store that is quietly counting the wrong thing.
/// </summary>
public class when_lowering_the_declaration : Specification
{
    IReadOnlyCollection<ProjectionDefinition> _projections;
    ProjectionDefinition _namespaced;
    ProjectionDefinition _global;

    void Because()
    {
        (_, _projections) = KernelProjectionDeclarations.Lower(typeof(EventTypeStatisticsProjection));
        _namespaced = _projections.Single(_ => _.Scope == ProjectionScope.Namespaced);
        _global = _projections.Single(_ => _.Scope == ProjectionScope.Global);
    }

    [Fact] void should_lower_to_a_namespaced_and_a_global_half() => _projections.Count.ShouldEqual(2);

    [Fact] void should_count_every_event_rather_than_named_ones() => _projections.ShouldContainOnly(_projections.Where(_ => _.SubscribesToAllEvents));

    [Fact] void should_count_into_the_count_property() =>
        _namespaced.FromEvery.Properties[new(nameof(EventTypeStatistics.Count))].ShouldEqual(WellKnownExpressions.Count);

    /// <summary>
    /// Keyed by event type and namespace rather than by the event source, which is what makes this a projection
    /// rather than a scan: an event folds into its pair's row on append instead of being counted on read.
    /// </summary>
    [Fact] void should_key_the_namespaced_half_by_event_type_and_namespace() =>
        _namespaced.FromEvery.Key.Value.ShouldContain(WellKnownExpressions.Composite);

    [Fact] void should_key_the_global_half_the_same_way() =>
        _global.FromEvery.Key.ShouldEqual(_namespaced.FromEvery.Key);

    [Fact] void should_write_both_halves_to_the_same_read_model() =>
        _global.ReadModel.ShouldEqual(_namespaced.ReadModel);

    [Fact] void should_be_owned_by_the_kernel() => _projections.ShouldContainOnly(_projections.Where(_ => _.IsKernelOwned));

    /// <summary>
    /// A kernel projection that could be replayed would be a kernel projection someone can replay.
    /// </summary>
    [Fact] void should_not_be_replayable() => _projections.ShouldContainOnly(_projections.Where(_ => !_.IsRewindable));
}
