// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.given;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_with_a_superseded_timestamp_write : an_observer_entering_quarantine
{
    readonly held_storage_operation<bool> _timestamp = new();
    readonly blockable_state_storage<ObserverDefinition> _definition = new();
    readonly DeliveryClock _clock = new();
    protected readonly DateTimeOffset _now = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    protected DateTimeOffset? _newerTimestamp;
    protected Exception? _writeError;
    protected readonly List<DateTimeOffset?> _savedTimestamps = [];

    public an_observer_with_a_superseded_timestamp_write()
    {
        _clock.Advance(_now - DateTimeOffset.UnixEpoch);
        _timeProvider = _clock;
        _silo.Options.StorageFactory = type => type == typeof(ObserverDefinition) ? _definition : null!;
    }

    void Establish()
    {
        var first = true;
        _eventStoreNamespaceStorage.Observers.SaveRunningStateChangedAt(Arg.Any<ObserverId>(), Arg.Any<DateTimeOffset?>(), Arg.Any<EventSequenceId>()).Returns(call =>
        {
            var changedAt = call.ArgAt<DateTimeOffset?>(1);
            _savedTimestamps.Add(changedAt);
            if (first)
            {
                first = false;
                return _timestamp.Invoke();
            }
            _persistedRunningStateChangedAt = changedAt;
            return Task.FromResult(true);
        });
    }

    protected async Task SupersedeTimestampWrite(bool result, Exception? failure = null)
    {
        // Quarantine entry is awaiting cleanup: an interleaved progress write can persist its pending time.
        var writing = _observer.SetHandledStats(42UL);
        Task? reloading = null;
        try
        {
            await _timestamp.WaitForStart();
            _definition.SuspendNextRead = true;
            reloading = _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero, automatic: true);
            await _definition.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            _newerTimestamp = (await _observer.GetInformation()).RunningStateChangedAt;
            if (failure is null) _timestamp.Complete(result);
            else _timestamp.Fail(failure);
            _writeError = await Catch.Exception(() => writing.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System));

            // The reload owns new debt even though it retained the same quarantine timestamp.
            await _observer.SetHandledStats(43UL);
        }
        finally
        {
            _timestamp.Complete(result);
            _definition.ReleaseRead.TrySetResult();
            _cleanupJobs.TrySetResult(_jobs);
            await Catch.Exception(() => writing.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System));
            if (reloading is not null) await reloading.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            await _quarantineEntry.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
        await _observer.SetHandledStats(44UL);
    }
}
