// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Microsoft.Extensions.Logging;
using Orleans.TestKit;

namespace Cratis.Chronicle.Clients.for_ClientDepartureNotifier;

public class when_notifying_for_several_connections_at_once : Specification
{
    const int Cap = ClientDepartureNotifier.MaxConcurrentNotifications;
    const int ObserversPerConnection = Cap * 2;
    static readonly TimeSpan _gateTimeout = TimeSpan.FromSeconds(30);

    readonly Dictionary<string, IObserver> _observers = [];
    readonly object _gate = new();
    readonly SemaphoreSlim _entered = new(0);
    readonly SemaphoreSlim _step = new(0);
    ClientDepartureNotifier _notifier;
    ObserverKey[] _firstKeys;
    ObserverKey[] _secondKeys;
    int _inFlight;
    int _peakInFlight;

    void Establish()
    {
        var silo = new TestKitSilo();
        _firstKeys = CreateKeys("first");
        _secondKeys = CreateKeys("second");
        foreach (var key in _firstKeys.Concat(_secondKeys))
        {
            var observer = Substitute.For<IObserver>();
            observer.UnsubscribeIfMatchesClient(Arg.Any<ConnectionId>()).Returns(async _ =>
            {
                lock (_gate)
                {
                    _inFlight++;
                    _peakInFlight = Math.Max(_peakInFlight, _inFlight);
                }

                _entered.Release();
                await _step.WaitAsync(_gateTimeout);
                lock (_gate)
                {
                    _inFlight--;
                }
            });
            _observers[key.ToString()] = observer;
        }

        silo.AddProbe<IObserver>(identity => _observers[identity.ToString()]);
        _notifier = new ClientDepartureNotifier(silo.GrainFactory, Substitute.For<ILogger<ClientDepartureNotifier>>());
    }

    async Task Because()
    {
        const int total = ObserversPerConnection * 2;
        Task[] notifications = [];
        try
        {
            notifications =
            [
                Task.Run(() => _notifier.Notify("first-connection", _firstKeys)),
                Task.Run(() => _notifier.Notify("second-connection", _secondKeys))
            ];

            // Fill every slot, then admit exactly one more observer each time a slot is freed.
            for (var index = 0; index < Cap; index++)
            {
                await WaitForEntry();
            }

            for (var index = Cap; index < total; index++)
            {
                _step.Release();
                await WaitForEntry();
            }
        }
        finally
        {
            _step.Release(total);
            await Task.WhenAll(notifications).WaitAsync(_gateTimeout);
        }
    }

    [Fact] void should_notify_every_observer_with_its_connection_exactly_once() =>
        _observers.Values.ShouldEachConformTo(observer => Received(observer, "first-connection") + Received(observer, "second-connection") == 1);

    [Fact] void should_notify_first_connection_observers_with_the_first_connection() =>
        _firstKeys.ShouldEachConformTo(key => Received(_observers[key.ToString()], "first-connection") == 1);

    [Fact] void should_notify_second_connection_observers_with_the_second_connection() =>
        _secondKeys.ShouldEachConformTo(key => Received(_observers[key.ToString()], "second-connection") == 1);

    [Fact] void should_fill_the_cap_across_both_connections() => _peakInFlight.ShouldEqual(Cap);
    [Fact] void should_never_exceed_the_cap_across_all_connections() => _peakInFlight.ShouldBeLessThanOrEqual(Cap);

    static ObserverKey[] CreateKeys(string prefix) =>
        [.. Enumerable.Range(0, ObserversPerConnection).Select(index => new ObserverKey($"{prefix}-observer-{index}", "the-store", "the-namespace", EventSequenceId.Log))];

    static int Received(IObserver observer, string connection) =>
        observer.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IObserver.UnsubscribeIfMatchesClient) && (ConnectionId)call.GetArguments()[0]! == connection);

    async Task WaitForEntry()
    {
        if (!await _entered.WaitAsync(_gateTimeout))
        {
            throw new TimeoutException("An observer notification did not start; the cap may be blocking progress.");
        }
    }
}
