// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Cratis.Chronicle.Server.Authentication.for_IdentityEndpointAuthorization;

public class when_applying_login_metadata : Specification
{
    RouteEndpointBuilder _endpoint;

    void Establish() =>
        _endpoint = new RouteEndpointBuilder(
            _ => Task.CompletedTask,
            RoutePatternFactory.Parse("/identity/login"),
            order: 0);

    void Because() => IdentityEndpointAuthorization.Apply(_endpoint);

    [Fact] void should_allow_anonymous_access() => _endpoint.Metadata.OfType<IAllowAnonymous>().ShouldNotBeEmpty();
    [Fact] void should_ignore_antiforgery() => _endpoint.Metadata.OfType<IgnoreAntiforgeryTokenAttribute>().ShouldNotBeEmpty();
}
