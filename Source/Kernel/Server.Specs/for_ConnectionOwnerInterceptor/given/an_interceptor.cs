// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Configuration;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.for_ConnectionOwnerInterceptor.given;

public class an_interceptor : Specification
{
    protected readonly ConnectionOwnerResolver _owners = new(Options.Create(new ChronicleOptions()));
    protected readonly ClaimsPrincipal _caller = new(new ClaimsIdentity([new Claim("sub", "caller")], "test"));
    protected readonly ServerCallContext _context = Substitute.ForPartsOf<ServerCallContext>();
    protected Grpc.Core.Interceptors.Interceptor _interceptor;
    protected ClaimsPrincipal? _duringCall;

    void Establish()
    {
        _context.Method.Returns("/Cratis.Chronicle.Contracts.Clients.ConnectionService/ConnectionKeepAlive");
        _context.UserState["__HttpContext"] = new DefaultHttpContext { User = _caller };
        _interceptor = new ConnectionOwnerInterceptor(_owners);
    }
}
