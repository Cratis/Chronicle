// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.given;

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

    protected override Observers CreateObserversConfig() => new() { WriteCoalescingInterval = 0, IdleDeactivationAfter = IdleDeactivationAfter };

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
}
