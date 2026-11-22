// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.Clients.for_ConnectedClientsVisibility.when_filtering;

public class for_an_administrator_decided_from_the_callers_services : given.a_visibility
{
    object? _resource;
    IEnumerable<ConnectedClient> _result;

    void Establish() =>
        _authorization
            .AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), WellKnownAuthorizationPolicies.Administrator)
            .Returns(call =>
            {
                _resource = call.ArgAt<object?>(1);
                return AuthorizationResult.Success();
            });

    async Task Because() => _result = await _visibility.Filter(_clients, CallerFor(Alice));

    [Fact] void should_ask_about_a_request_carrying_services() => ((_resource as HttpContext)?.RequestServices).ShouldNotBeNull();
    [Fact] void should_see_every_connection() => _result.Count().ShouldEqual(2);
}
