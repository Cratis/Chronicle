// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.Server.for_UserIdentityMiddleware;

public class when_an_authenticated_principal_has_no_subject : Specification
{
    RequestDelegate _next;
    DefaultHttpContext _context;
    UserIdentityMiddleware _middleware;

    void Establish()
    {
        _next = Substitute.For<RequestDelegate>();
        _context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", "  ")],
                authenticationType: "Test"))
        };
        _middleware = new UserIdentityMiddleware(_next);
    }

    async Task Because() => await _middleware.InvokeAsync(_context);

    [Fact] void should_return_unauthorized() => _context.Response.StatusCode.ShouldEqual(StatusCodes.Status401Unauthorized);
    [Fact] void should_not_continue() => _next.DidNotReceive().Invoke(_context);
}
