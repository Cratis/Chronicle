// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.with_real_ownership;

public class a_caller_with_only_a_client_id : given.a_service_with_real_ownership
{
    Exception _opened;
    Exception _ownKeepAlive;
    Exception _otherKeepAlive;
    Exception _otherOpened;

    async Task Because()
    {
        _opened = await Open(AuthenticatedCalls.AsClient("app"));
        _ownKeepAlive = await KeepAlive(AuthenticatedCalls.AsClient("app"));
        _otherKeepAlive = await KeepAlive(AuthenticatedCalls.AsClient("other"));
        _otherOpened = await Open(AuthenticatedCalls.AsClient("other"));
    }

    [Fact] void should_connect() => _opened.ShouldBeNull();
    [Fact] void should_own_the_connection() => _ownership.IsOwnedBy(Id, AuthenticatedCalls.OwnerOfClient("app")).ShouldBeTrue();
    [Fact] void should_accept_its_own_keep_alive() => _ownKeepAlive.ShouldBeNull();
    [Fact] void should_refuse_a_keep_alive_from_another_client() => ((RpcException)_otherKeepAlive).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] void should_refuse_a_connection_from_another_client() => ((RpcException)_otherOpened).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
}
