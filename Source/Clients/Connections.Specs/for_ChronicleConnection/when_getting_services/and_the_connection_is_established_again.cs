// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_getting_services;

public class and_the_connection_is_established_again : given.a_connection_factory
{
    IServices _first;
    IServices _second;

    void Establish() => _connection = CreateConnection(new ChronicleConnectionString("chronicle://localhost:35000/?skipKeepAlive=true"));

    async Task Because()
    {
        _first = await _connection.GetServices(CancellationToken.None);
        _second = await _connection.GetServices(CancellationToken.None);
    }

    [Fact] void should_hand_out_a_new_service_container() => ReferenceEquals(_second, _first).ShouldBeFalse();
    [Fact] void should_create_proxies_bound_to_the_new_connection() => ReferenceEquals(_second.Sequences, _first.Sequences).ShouldBeFalse();
}
