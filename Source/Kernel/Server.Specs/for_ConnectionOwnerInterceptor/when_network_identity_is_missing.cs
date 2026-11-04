// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Clients;
using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.Server.for_ConnectionOwnerInterceptor;

public class when_network_identity_is_missing : given.an_interceptor
{
    ConnectionOwner _duringCallOwner;

    void Establish()
    {
        var http = Substitute.For<HttpContext>();
        http.User.Returns((ClaimsPrincipal)null!);
        _context.UserState["__HttpContext"] = http;
    }

    Task Because() => _interceptor.UnaryServerHandler<string, string>("request", _context, (_, _) =>
    {
        _duringCallOwner = _owners.Current();
        return Task.FromResult("response");
    });

    [Fact] void should_deny_instead_of_resolving_in_process() => _duringCallOwner.ShouldEqual(ConnectionOwner.Unidentified);
    [Fact] void should_clear_the_transport_marker() => _owners.HasTransport.ShouldBeFalse();
}
