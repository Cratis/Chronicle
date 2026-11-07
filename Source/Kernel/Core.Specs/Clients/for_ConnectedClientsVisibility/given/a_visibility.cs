// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Clients.for_ConnectedClientsVisibility.given;

public class a_visibility : Specification
{
    protected static readonly ConnectionOwner Alice = new("issuer", "alice");
    protected static readonly ConnectionOwner Bob = new("issuer", "bob");
    protected IConnectionOwnership _ownership;
    protected ConnectedClientsVisibility _visibility;
    protected IAuthorizationService _authorization;
    protected IEnumerable<ConnectedClient> _clients;
    protected IEnumerable<ConnectedClient> _result;

    protected virtual bool AuthenticationEnabled => true;

    void Establish()
    {
        _ownership = Substitute.For<IConnectionOwnership>();
        _ownership.IsOwnedBy("alice-connection", Alice).Returns(true);
        _ownership.IsOwnedBy("bob-connection", Bob).Returns(true);
        _authorization = Substitute.For<IAuthorizationService>();
        _authorization
            .AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(AuthorizationResult.Failed());
        _authorization
            .AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>())
            .Returns(AuthorizationResult.Failed());
        var options = Options.Create(new ChronicleOptions { Authentication = new() { Enabled = AuthenticationEnabled } });
        _visibility = new(options, _ownership, new Microsoft.AspNetCore.Http.HttpContextAccessor());
        _clients = [new ConnectedClient { ConnectionId = "alice-connection" }, new ConnectedClient { ConnectionId = "bob-connection" }];
    }

    protected ConnectionCaller CallerFor(ConnectionOwner owner)
    {
        var services = new ServiceCollection().AddSingleton(_authorization).BuildServiceProvider();
        return new(owner, new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", owner.Subject)], "test")), services);
    }

    protected void MakeAdministrator() =>
        _authorization
            .AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), WellKnownAuthorizationPolicies.Administrator)
            .Returns(AuthorizationResult.Success());
}
