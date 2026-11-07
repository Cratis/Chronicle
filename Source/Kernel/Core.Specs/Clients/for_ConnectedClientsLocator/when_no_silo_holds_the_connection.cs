// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectedClientsLocator;

public class when_no_silo_holds_the_connection : given.two_silos
{
    IConnectedClients? _result;

    async Task Because() => _result = await ConnectedClientsLocator.Find(_grainFactory, _first, _connection);

    [Fact] void should_find_nothing() => _result.ShouldBeNull();
}
