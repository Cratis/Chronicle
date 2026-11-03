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
    const int ObserversPerConnection = ClientDepartureNotifier.MaxConcurrentNotifications * 2;

    readonly Dictionary<string, IObserver> _observers = [];
    readonly object _gate = new();
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

                await Task.Delay(20);
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

    Task Because() => Task.WhenAll(
        Task.Run(() => _notifier.Notify("first-connection", _firstKeys)),
        Task.Run(() => _notifier.Notify("second-connection", _secondKeys)));

    [Fact] void should_notify_every_observer() => _observers.Count.ShouldEqual(ObserversPerConnection * 2);
    [Fact] void should_have_notified_concurrently() => _peakInFlight.ShouldBeGreaterThan(1);
    [Fact] void should_bound_in_flight_notifications_across_all_connections() => _peakInFlight.ShouldBeLessThanOrEqual(ClientDepartureNotifier.MaxConcurrentNotifications);

    static ObserverKey[] CreateKeys(string prefix) =>
        [.. Enumerable.Range(0, ObserversPerConnection).Select(index => new ObserverKey($"{prefix}-observer-{index}", "the-store", "the-namespace", EventSequenceId.Log))];
}
