// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_a_newer_client_registers_on_another_sequence : given.an_observer_with_a_versioned_owner_on_another_sequence
{
    async Task Because() => await Register();

    [Fact] void should_move_the_definition() => _definitionStorage.State.EventSequenceId.ShouldEqual(_observerKey.EventSequenceId);
    [Fact] void should_not_recommend() => ShouldNotRecommend();
    [Fact] void should_report_the_newer_registration_switch() => ((given.RecordingObserverLogger)_logger).Information.Any(message => message.Contains("IncomingIsNewer")).ShouldBeTrue();
}
