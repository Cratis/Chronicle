// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_activated;

public class and_the_key_does_not_match_the_definition : given.an_observer_under_a_mismatching_key
{
    async Task Because()
    {
        await Crash(resetStorageStatistics: false);
        await _observer.RunWatchdogAsync();
        await ReportAlerts();
        await _observer.Unsubscribe();
        await _observer.OnDeactivateAsync(new DeactivationReason(DeactivationReasonCode.ApplicationRequested, "Spec"), default);
    }

    [Fact] void should_not_write_state() => _storageStats.Writes.ShouldEqual(0);
    [Fact] void should_not_write_the_definition() => _definitionStorageStats.Writes.ShouldEqual(0);
    [Fact] void should_keep_the_running_state_of_the_real_observer() => _persistedStateStorage!.PersistedState.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_keep_the_definition_on_its_event_sequence() => _definitionStorage.State.EventSequenceId.ShouldEqual(_definitionEventSequenceId);
    [Fact] void should_not_register_a_reminder() => _silo.ReminderRegistry.Mock.Verify(registry => registry.RegisterOrUpdateReminder(It.IsAny<GrainId>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()), Times.Never);
}
