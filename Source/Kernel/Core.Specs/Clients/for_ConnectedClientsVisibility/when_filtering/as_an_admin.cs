// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Clients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Clients.for_ConnectedClientsVisibility.when_filtering;

public class as_an_admin : Specification
{
    IEnumerable<ConnectedClient> _clients;

    async Task Because()
    {
        var caller = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "admin")], "test"));
        var options = Options.Create(new ChronicleOptions());
        var owners = new ConnectionOwnerResolver(options);
        var authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(caller, null, Chronicle.Security.WellKnownAuthorizationPolicies.Administrator).Returns(AuthorizationResult.Success());
        var visibility = new ConnectedClientsVisibility(options, owners, new ConnectionOwnership(), authorization, new HttpContextAccessor());
        _clients = await visibility.Capture(caller)([new() { ConnectionId = "one" }, new() { ConnectionId = "two" }]);
    }

    [Fact] void should_list_every_connection() => _clients.Count().ShouldEqual(2);
}
