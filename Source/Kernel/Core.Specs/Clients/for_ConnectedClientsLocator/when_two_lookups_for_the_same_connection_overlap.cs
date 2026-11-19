// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectedClientsLocator;

public class when_two_lookups_for_the_same_connection_overlap : given.two_silos
{
    TaskCompletionSource<bool> _answer;
    IConnectedClients? _firstResult;
    IConnectedClients? _secondResult;

    void Establish()
    {
        _answer = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _firstClients.IsConnected(_connection).Returns(_answer.Task);
    }

    async Task Because()
    {
        var first = ConnectedClientsLocator.Find(_grainFactory, _first, _connection);
        var second = ConnectedClientsLocator.Find(_grainFactory, _first, _connection);
        _answer.SetResult(true);
        _firstResult = await first;
        _secondResult = await second;
    }

    [Fact] void should_ask_the_registry_once() => _firstClients.Received(1).IsConnected(_connection);
    [Fact] void should_find_the_registry_for_the_first_lookup() => _firstResult.ShouldEqual(_firstClients);
    [Fact] void should_find_the_registry_for_the_second_lookup() => _secondResult.ShouldEqual(_firstClients);
}
