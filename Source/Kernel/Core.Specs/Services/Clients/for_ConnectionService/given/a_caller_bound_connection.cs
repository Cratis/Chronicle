// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Configuration;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.given;

public class a_caller_bound_connection : Specification
{
    protected readonly ConnectionOwnership _ownership = new();
    protected readonly ConnectionOwnerResolver _owners = new(Options.Create(new ChronicleOptions()));
    protected readonly ClaimsPrincipal _caller = new(new ClaimsIdentity([new Claim("sub", "other"), new Claim("iss", "issuer")], "test"));
    protected Contracts.Clients.IConnectionService _service;
    protected CallContext _context;
    protected Exception _error;

    void Establish()
    {
        _ownership.Claim("connection", new("original"));
        var context = Substitute.ForPartsOf<ServerCallContext>();
        context.UserState["__HttpContext"] = new DefaultHttpContext { User = _caller };
        _context = new(this, context);
        var options = Options.Create(new ChronicleOptions());
        var grains = Substitute.For<IGrainFactory>();
        _service = new ConnectionService(grains, Substitute.For<ILocalSiloDetails>(), new ConnectedClientsQuery(grains, options), new OpenConnections(), NullLogger<ConnectionService>.Instance, options, _ownership, _owners, Substitute.For<IConnectedClientsVisibility>());
    }
}
