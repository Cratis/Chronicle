// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Clients.for_ClientConnection.when_connecting;

public class after_disconnect : given.a_client_connection
{
    ClientConnectionEpoch _firstEpoch;
    ClientConnectionEpoch _secondEpoch;

    async Task Establish()
    {
        await _connection.Connect();
        _firstEpoch = await _connection.Subscribe(_first);
        await _connection.Disconnect();
    }

    async Task Because()
    {
        await _connection.Connect();
        _secondEpoch = await _connection.Subscribe(_first);
    }

    [Fact] void should_have_a_new_epoch() => _secondEpoch.ShouldNotEqual(_firstEpoch);
    [Fact] void should_not_be_not_set() => _secondEpoch.ShouldNotEqual(ClientConnectionEpoch.NotSet);
}
