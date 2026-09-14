// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.Server.Authentication.for_CookieAntiforgeryMiddleware.given;

public class a_cookie_antiforgery_middleware : Specification
{
    protected RequestDelegate _next;
    protected IAntiforgery _antiforgery;
    protected DefaultHttpContext _context;
    protected CookieAntiforgeryMiddleware _middleware;

    void Establish()
    {
        _next = Substitute.For<RequestDelegate>();
        _antiforgery = Substitute.For<IAntiforgery>();
        _context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", "operator")],
                authenticationType: "Cookie"))
        };
        _context.Request.Headers.Cookie = $"{CookieAntiforgeryMiddleware.AuthenticationCookieName}=value";
        _middleware = new CookieAntiforgeryMiddleware(_next);
    }
}
