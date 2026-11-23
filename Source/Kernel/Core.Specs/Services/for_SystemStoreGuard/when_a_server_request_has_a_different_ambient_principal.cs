// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Services.for_QueryExecutor;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.for_SystemStoreGuard;

public class when_a_server_request_has_a_different_ambient_principal : given.an_in_process_request
{
    IAuthorizationDecisions _decisions;
    ClaimsPrincipal _serverCaller;
    CallContext _context;
    bool _permitted;

    void Establish()
    {
        _decisions = _httpContext.RequestServices.GetRequiredService<IAuthorizationDecisions>();
        _serverCaller = CallerNamed(OrdinaryUser);
        var request = new DefaultHttpContext { User = _serverCaller, RequestServices = _httpContext.RequestServices };
        _context = new(new object(), new FakeServerCallContext(request));
    }

    async Task Because()
    {
        BeginRequest();
        _permitted = await StoreCapabilityGuard.Permits(_context, new QueryRouting("shop", "tenant"));
    }

    [Fact] void should_permit_the_entitled_caller() => _permitted.ShouldBeTrue();
    [Fact] async Task should_use_the_server_principal() => await _decisions.Received(1).Decide(
        _serverCaller, WellKnownCapabilities.ReadEventStore, AuthorizationScope.ForNamespace("shop", "tenant"));
    [Fact] async Task should_not_substitute_the_ambient_principal() => await _decisions.DidNotReceive().Decide(
        _httpContext.User, Arg.Any<Capability>(), Arg.Any<AuthorizationScope>());
}
