// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Placement;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber;

public class when_inspecting_placement : Specification
{
    [Fact] void should_prefer_local_placement() => typeof(ProjectionObserverSubscriber).IsDefined(typeof(PreferLocalPlacementAttribute), false).ShouldBeTrue();
    [Fact] void should_have_only_the_builtin_placement_strategy() => typeof(ProjectionObserverSubscriber).GetCustomAttributes(typeof(PlacementAttribute), false).Length.ShouldEqual(1);
}
