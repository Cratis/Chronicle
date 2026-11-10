// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.given;
using Cratis.Chronicle.Storage.Observation;
using Moq;
using Orleans.TestKit.Storage;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs.given;

/// <summary>
/// An observer with no subscription and nothing waiting, whose watchdog releases it after a minute of being idle.
/// </summary>
public class an_idle_observer : for_Observer.given.an_observer
{
    protected static readonly TimeSpan _idlePeriod = TimeSpan.FromSeconds(60);
    protected readonly DeliveryClock _clock = new();

    public an_idle_observer() => _timeProvider = _clock;

    /// <summary>
    /// Gets the configured idle period in seconds.
    /// </summary>
    protected virtual int IdleDeactivationAfter => (int)_idlePeriod.TotalSeconds;

    /// <summary>
    /// Gets the configured write coalescing interval in seconds.
    /// </summary>
    protected virtual int WriteCoalescingInterval => 0;

    /// <summary>
    /// Gets the number of times the observer has asked Orleans to deactivate it.
    /// </summary>
    protected int ReleaseRequests => _silo.GrainRuntime.Mock.Invocations.Count(invocation => invocation.Method.Name == nameof(IGrainRuntime.DeactivateOnIdle));

    protected override Observers CreateObserversConfig() => new() { WriteCoalescingInterval = WriteCoalescingInterval, IdleDeactivationAfter = IdleDeactivationAfter };

    /// <summary>
    /// The first alert reminder tick finds nothing to report and gives the reminder up, as it does for a settled observer.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    async Task Establish() => await _observer.ReceiveReminder(Observer.AlertReminderName, default);

    protected ObserverSubscription ASubscription() =>
        new(_observerId, _observerKey, [EventType.Unknown], typeof(ObserverSubscriber), SiloAddress.Zero, null);

    protected async Task RunWatchdogAfter(TimeSpan elapsed)
    {
        _clock.Advance(elapsed);
        await _observer.RunWatchdogAsync();
    }

    /// <summary>
    /// Activates the observer again from what is stored, and lets it settle its alerts the way a settled observer does.
    /// </summary>
    /// <param name="stored">Changes the stored observer before the activation loads it.</param>
    /// <returns>Awaitable task.</returns>
    protected async Task ActivateAgainFrom(Func<ObserverState, ObserverState> stored)
    {
        _stateStorage.State = stored(_stateStorage.State);
        await Crash();
        await _observer.ReportAlertState();
        await _observer.ReceiveReminder(Observer.AlertReminderName, default);
        _silo.GrainRuntime.Mock.Invocations.Clear();
    }

    protected void ResetRecordedWork()
    {
        _storageStats.ResetCounts();
        _definitionStorageStats.ResetCounts();
        _observerAlerts.ClearReceivedCalls();
        _silo.ReminderRegistry.Mock.Invocations.Clear();
    }

    protected TestStorageStats _definitionStorageStats => _silo.StorageManager.GetStorageStats(nameof(ObserverDefinition))!;

    protected void ShouldNotHaveRegisteredAnyReminder() => _silo.ReminderRegistry.Mock.Verify(
        registry => registry.RegisterOrUpdateReminder(It.IsAny<GrainId>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()),
        Times.Never);
}
