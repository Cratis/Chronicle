// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_a_client_with_the_same_version_registers_on_another_sequence : given.an_observer_with_a_versioned_owner_on_another_sequence
{
    void Establish() => _client = _client with { Version = "1.0.0" };
    async Task Because() => await Register();

    [Fact] void should_move_the_definition() => _definitionStorage.State.EventSequenceId.ShouldEqual(_observerKey.EventSequenceId);
    [Fact] async Task should_recommend_inspecting_the_switch() => await ShouldRecommendAnUnorderedSwitch();
    [Fact] void should_warn_about_the_switch() => ((given.RecordingObserverLogger)_logger).Warnings.Any(message => message.Contains("without version ordering")).ShouldBeTrue();
}
