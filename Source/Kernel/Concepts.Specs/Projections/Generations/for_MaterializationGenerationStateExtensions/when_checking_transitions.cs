// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations.for_MaterializationGenerationStateExtensions;

public class when_checking_transitions : Specification
{
    [Fact] void should_allow_building_to_following() => MaterializationGenerationState.Building.CanTransitionTo(MaterializationGenerationState.Following).ShouldBeTrue();
    [Fact] void should_allow_building_to_be_abandoned() => MaterializationGenerationState.Building.CanTransitionTo(MaterializationGenerationState.Abandoned).ShouldBeTrue();
    [Fact] void should_not_allow_building_to_become_active() => MaterializationGenerationState.Building.CanTransitionTo(MaterializationGenerationState.Active).ShouldBeFalse();
    [Fact] void should_allow_following_to_become_active() => MaterializationGenerationState.Following.CanTransitionTo(MaterializationGenerationState.Active).ShouldBeTrue();
    [Fact] void should_not_allow_active_to_be_abandoned() => MaterializationGenerationState.Active.CanTransitionTo(MaterializationGenerationState.Abandoned).ShouldBeFalse();
    [Fact] void should_allow_retired_to_become_active_again() => MaterializationGenerationState.Retired.CanTransitionTo(MaterializationGenerationState.Active).ShouldBeTrue();
    [Fact] void should_not_allow_leaving_removed() => MaterializationGenerationState.Removed.CanTransitionTo(MaterializationGenerationState.Building).ShouldBeFalse();
    [Fact] void should_not_treat_building_as_following_live_events() => MaterializationGenerationState.Building.IsFollowingLiveEvents().ShouldBeFalse();
    [Fact] void should_treat_retired_as_following_live_events() => MaterializationGenerationState.Retired.IsFollowingLiveEvents().ShouldBeTrue();
}
