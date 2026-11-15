// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Clients;
using ConnectionId = Cratis.Chronicle.Concepts.Clients.ConnectionId;
using KernelConnectedClient = Cratis.Chronicle.Concepts.Clients.ConnectedClient;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_getting_connected_clients;

public class as_a_non_administrator : given.a_service_with_real_ownership
{
    IEnumerable<ConnectedClient> _result;
    IEnumerable<ConnectedClient> _administratorResult;

    async Task Establish()
    {
        _connectedClients.GetAllConnectedClients().Returns(Task.FromResult<IEnumerable<KernelConnectedClient>>(
        [
            new() { ConnectionId = new ConnectionId(Id) },
            new() { ConnectionId = new ConnectionId("another-connection") }
        ]));
        await Open(AuthenticatedCalls.As("alice"));
    }

    async Task Because()
    {
        _result = await _service.GetConnectedClients(AuthenticatedCalls.As("alice"));
        _administratorResult = await _service.GetConnectedClients(AuthenticatedCalls.As("admin", administrator: true));
    }

    [Fact] void should_see_only_its_own_connection() => _result.Select(_ => _.ConnectionId).ShouldContainOnly(Id);
    [Fact] async Task should_not_let_another_principal_see_it() => (await _service.GetConnectedClients(AuthenticatedCalls.As("bob"))).ShouldBeEmpty();
    [Fact] async Task should_show_an_unidentified_caller_nothing() => (await _service.GetConnectedClients(AuthenticatedCalls.Unauthenticated())).ShouldBeEmpty();
    [Fact] void should_show_an_administrator_everything() => _administratorResult.Count().ShouldEqual(2);
}
