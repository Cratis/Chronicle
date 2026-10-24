// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.for_Observer.when_restoring_client_subscription;

public class and_the_client_is_already_subscribed : given.an_observer_that_lost_its_client
{
    readonly RecordingLogger<Observer> _recordedLogs = new();

    protected override ILogger<Observer> CreateLogger() => _recordedLogs;

    async Task Establish()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [event_type], SiloAddress.Zero, _client);
        _namespaces.ClearReceivedCalls();
        _storageStats.ResetCounts();
    }

    async Task Because()
    {
        await _observer.RestoreClientSubscription<NullObserverSubscriber>(ObserverType.Reactor, [event_type], SiloAddress.Zero, _client);
        await _observer.RestoreClientSubscription<NullObserverSubscriber>(ObserverType.Reactor, [event_type], SiloAddress.Zero, _client);
    }

    [Fact] void should_log_only_the_first_rejection() => _recordedLogs.Messages
        .Count(message => message.Contains("Client already known", StringComparison.Ordinal))
        .ShouldEqual(1);

    [Fact] void should_not_write_state() => _storageStats.Writes.ShouldEqual(0);
    [Fact] async Task should_not_look_up_namespaces() => await _namespaces.DidNotReceive().GetAll();
    [Fact] async Task should_keep_a_single_target() => (await _observer.GetSubscription()).Targets.Count.ShouldEqual(1);
}
