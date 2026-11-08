// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Contracts.Clients;
using Grpc.Core;

namespace Cratis.Chronicle.Server.for_ConnectionOwnership;

public class when_an_authenticated_client_connects_over_grpc : given.an_authenticated_grpc_connection
{
    Exception? _first;
    Exception? _reconnect;
    Exception? _keepAlive;
    Exception? _otherCaller;
    Exception? _unidentified;

    async Task Because()
    {
        _first = await Open("alice");
        _reconnect = await Open("alice");
        _keepAlive = await Catch.Exception(() => _client.ConnectionKeepAlive(new ConnectionKeepAlive { ConnectionId = Id }, ContextFor("alice")));
        _otherCaller = await Catch.Exception(() => _client.ConnectionKeepAlive(new ConnectionKeepAlive { ConnectionId = Id }, ContextFor("bob")));
        _unidentified = await Catch.Exception(() => _client.ConnectionKeepAlive(new ConnectionKeepAlive { ConnectionId = Id }, ContextFor(string.Empty)));
    }

    [Fact] void should_open_the_connection() => _first.ShouldBeNull();
    [Fact] void should_allow_the_same_identity_to_reconnect() => _reconnect.ShouldBeNull();
    [Fact] void should_accept_the_owners_keep_alive() => _keepAlive.ShouldBeNull();
    [Fact] void should_bind_the_connection_to_the_authenticated_identity() => _ownership.IsOwnedBy(Id, new ConnectionOwner("issuer", "alice")).ShouldBeTrue();
    [Fact] void should_refuse_a_different_caller() => ((RpcException)_otherCaller!).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] void should_refuse_an_unidentified_caller() => ((RpcException)_unidentified!).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
}
