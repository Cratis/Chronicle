// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Clients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Clients.for_ConnectedClientsVisibility.when_filtering;

public class as_a_non_admin : Specification
{
    readonly ClaimsPrincipal _caller = new(new ClaimsIdentity([new Claim("sub", "caller"), new Claim("iss", "issuer")], "test"));
    readonly ConnectionOwnership _ownership = new();
    IEnumerable<ConnectedClient> _clients;

    void Establish() => _ownership.Claim("owned", ConnectionOwner.From(_caller));
    async Task Because()
    {
        var options = Options.Create(new ChronicleOptions());
        var owners = new ConnectionOwnerResolver(options);
        var authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(_caller, null, Chronicle.Security.WellKnownAuthorizationPolicies.Administrator).Returns(AuthorizationResult.Failed());
        var visibility = new ConnectedClientsVisibility(options, owners, _ownership, authorization, new HttpContextAccessor());
        _clients = await visibility.Capture(_caller)([new() { ConnectionId = "owned" }, new() { ConnectionId = "other" }]);
    }

    [Fact] void should_only_list_owned_connections() => _clients.Select(_ => _.ConnectionId).ShouldEqual(["owned"]);
}
