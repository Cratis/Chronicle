// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.for_Observer.when_restoring_client_subscription;

public class and_the_rejection_diagnostic_changes : given.an_observer_that_lost_its_client
{
    readonly ConnectedClient _otherClient = new() { ConnectionId = "another-connection", Version = "1.0.0" };
    readonly SiloAddress _otherSilo = SiloAddress.FromParsableString("127.0.0.1:11111@1");

    readonly RecordingLogger<Observer> _recordedLogs = new();

    protected override ILogger<Observer> CreateLogger() => _recordedLogs;

    void Establish() => _namespaces.GetAll().Returns([]);

    async Task Because()
    {
        await RestoreTwice(_client, SiloAddress.Zero);
        await RestoreTwice(_otherClient, SiloAddress.Zero);
        await RestoreTwice(_otherClient, _otherSilo);

        // Returning to a previous tuple must log again: only the latest diagnostic is remembered.
        await RestoreTwice(_client, SiloAddress.Zero);
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [event_type], SiloAddress.Zero, _otherClient);
        await RestoreTwice(_otherClient, SiloAddress.Zero);
        await _observer.TransitionTo<QuarantinedObserver>();
        await RestoreTwice(_otherClient, SiloAddress.Zero);
        await RestoreTwice(_client, SiloAddress.Zero);
    }

    [Fact] void should_log_each_changed_connection_target_or_return_to_a_previous_diagnostic() => Messages()
        .Count(message => message.Contains("Namespace no longer exists", StringComparison.Ordinal)).ShouldEqual(4);
    [Fact] void should_log_a_state_change_even_when_the_client_target_and_reason_are_unchanged() => Messages()
        .Count(message => message.Contains("Client already known", StringComparison.Ordinal)).ShouldEqual(2);
    [Fact] void should_log_the_changed_lifecycle_reason_and_state() => Messages()
        .Count(message => message.Contains("Reconciliation prohibited by observer lifecycle", StringComparison.Ordinal) && message.Contains("Quarantined", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] async Task should_not_subscribe_the_client() => (await _observer.KnowsClient(_client.ConnectionId)).ShouldBeFalse();

    async Task RestoreTwice(ConnectedClient client, SiloAddress silo)
    {
        await _observer.RestoreClientSubscription<NullObserverSubscriber>(ObserverType.Reactor, [event_type], silo, client);
        await _observer.RestoreClientSubscription<NullObserverSubscriber>(ObserverType.Reactor, [event_type], silo, client);
    }

    IEnumerable<string> Messages() => _recordedLogs.Messages;
}
