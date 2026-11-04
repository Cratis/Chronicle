// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Clients.for_ConnectedClients.given;

public class a_registry_that_notifies_departures : Specification
{
    protected ConnectedClients _clients;
    protected IClientDepartureNotifier _notifier;
    protected ConnectionId _connection = ConnectionId.New();
    protected ConnectionId _otherConnection = ConnectionId.New();
    protected ObserverKey _first = Key("first");
    protected ObserverKey _second = Key("second");
    protected ObserverKey _third = Key("third");
    readonly Channel<(ConnectionId Connection, ObserverKey[] Observers)> _notifications = Channel.CreateUnbounded<(ConnectionId, ObserverKey[])>();

    void Establish()
    {
        _notifier = Substitute.For<IClientDepartureNotifier>();
        _notifier
            .Notify(Arg.Any<ConnectionId>(), Arg.Any<IReadOnlyCollection<ObserverKey>>())
            .Returns(call =>
            {
                _notifications.Writer.TryWrite((call.Arg<ConnectionId>(), call.Arg<IReadOnlyCollection<ObserverKey>>().ToArray()));
                return Task.CompletedTask;
            });
        _clients = new ConnectedClients(
            Substitute.For<ILogger<ConnectedClients>>(),
            Substitute.For<IMeter<ConnectedClients>>(),
            Options.Create(new ChronicleOptions()),
            Substitute.For<IStorage>(),
            _notifier);
    }

    protected static ObserverKey Key(string observerId) => new(observerId, "the-store", "the-namespace", EventSequenceId.Log);

    protected Task Connect(ConnectionId connection) => _clients.OnClientConnected(connection, "1.0", false, 1, "/app", "host", ".NET");

    protected Task Restore(ConnectionId connection) => _clients.OnClientRestored(connection, "1.0", false, 1, "/app", "host", ".NET");

    protected async Task<(ConnectionId Connection, ObserverKey[] Observers)> NextNotification() =>
        await _notifications.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
}
