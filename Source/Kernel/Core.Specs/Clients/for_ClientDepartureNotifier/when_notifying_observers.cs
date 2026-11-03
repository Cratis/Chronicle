// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Microsoft.Extensions.Logging;
using Orleans.TestKit;

namespace Cratis.Chronicle.Clients.for_ClientDepartureNotifier;

public class when_notifying_observers : Specification
{
    const int ObserverCount = (ClientDepartureNotifier.MaxConcurrentNotifications * 2) + 3;

    readonly ConnectionId _connection = "dropped-connection";
    readonly Dictionary<string, IObserver> _observers = [];
    readonly object _gate = new();
    ClientDepartureNotifier _notifier;
    ObserverKey[] _keys;
    int _inFlight;
    int _peakInFlight;
    string _failingObserverId;

    async Task Establish()
    {
        var silo = new TestKitSilo();
        _keys = [.. Enumerable.Range(0, ObserverCount).Select(index => new ObserverKey($"observer-{index}", "the-store", "the-namespace", EventSequenceId.Log))];
        _failingObserverId = _keys[1].ObserverId;
        foreach (var key in _keys)
        {
            var observer = Substitute.For<IObserver>();
            observer.UnsubscribeIfMatchesClient(Arg.Any<ConnectionId>()).Returns(async _ =>
            {
                lock (_gate)
                {
                    _inFlight++;
                    _peakInFlight = Math.Max(_peakInFlight, _inFlight);
                }

                await Task.Delay(1);
                lock (_gate)
                {
                    _inFlight--;
                }

                if (key.ObserverId == _failingObserverId)
                {
                    throw new IOException("Simulated unreachable observer");
                }
            });
            _observers[key.ToString()] = observer;
        }

        silo.AddProbe<IObserver>(identity => _observers[identity.ToString()]);
        _notifier = new ClientDepartureNotifier(silo.GrainFactory, Substitute.For<ILogger<ClientDepartureNotifier>>());
        await Task.CompletedTask;
    }

    Task Because() => _notifier.Notify(_connection, _keys);

    [Fact] void should_notify_every_observer_with_the_exact_connection() => _observers.Values.ShouldEachConformTo(observer => observer.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IObserver.UnsubscribeIfMatchesClient) && (ConnectionId)call.GetArguments()[0]! == _connection) == 1);
    [Fact] void should_keep_notifying_the_other_observers_after_an_observer_fails() => _keys.Where(key => key.ObserverId != _failingObserverId).ShouldEachConformTo(key => ReceivedFor(_observers[key.ToString()]) == 1);
    [Fact] void should_have_attempted_the_failing_observer_once() => ReceivedFor(_observers[_keys[1].ToString()]).ShouldEqual(1);
    [Fact] void should_bound_how_many_are_notified_at_once() => _peakInFlight.ShouldBeLessThanOrEqual(ClientDepartureNotifier.MaxConcurrentNotifications);

    int ReceivedFor(IObserver observer) =>
        observer.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IObserver.UnsubscribeIfMatchesClient) && (ConnectionId)call.GetArguments()[0]! == _connection);
}
