// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.XUnit.Integration.for_ChronicleConnection.given;
using IConnectedClients = KernelCore::Cratis.Chronicle.Clients.IConnectedClients;
using KernelConnectionId = Cratis.Chronicle.Concepts.Clients.ConnectionId;

namespace Cratis.Chronicle.XUnit.Integration.for_ChronicleConnection.when_disposing;

public class and_the_connection_is_established : an_in_process_connection
{
    readonly TaskCompletionSource _disconnected = new(TaskCreationOptions.RunContinuationsAsynchronously);
    IConnectedClients _connectedClients;
    Exception _disconnectError;
    Exception _repeatDisposeError;
    int _pingsAtDisconnection;
    int _pingsAfterDisconnection;

    void Establish()
    {
        _connectedClients = _grainFactory.GetGrain<IConnectedClients>(_siloDetails.SiloAddress.ToParsableString());
        _connectedClients.OnClientDisconnected(Arg.Any<KernelConnectionId>(), Arg.Any<string>())
            .Returns(_ =>
            {
                _disconnected.TrySetResult();
                return Task.CompletedTask;
            });
    }

    async Task Because()
    {
        await ((IChronicleConnection)_connection).Connect();
        _connection.Dispose();
        _repeatDisposeError = Catch.Exception(_connection.Dispose);
        _disconnectError = await Catch.Exception(() => _disconnected.Task.WaitAsync(TimeSpan.FromSeconds(10)));
        _pingsAtDisconnection = PingCount();

        var nextPing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _connectedClients.OnClientPing(Arg.Any<KernelConnectionId>()).Returns(_ =>
        {
            nextPing.TrySetResult();
            return Task.FromResult(true);
        });

        // Observe the absence of another ping across more than three default keep-alive intervals.
        await Catch.Exception(() => nextPing.Task.WaitAsync(TimeSpan.FromSeconds(3.5)));
        _pingsAfterDisconnection = PingCount();
    }

    int PingCount() => _connectedClients.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IConnectedClients.OnClientPing));

    [Fact] void should_end_the_connection_loop() => _disconnectError.ShouldBeNull();
    [Fact] void should_stop_pinging_the_registry() => _pingsAfterDisconnection.ShouldEqual(_pingsAtDisconnection);
    [Fact] void should_allow_repeated_disposal() => _repeatDisposeError.ShouldBeNull();
}
