// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Security;
using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.Server.Authentication.for_CookieAntiforgeryMiddleware;

public class when_handling_a_trusted_proxy_user_mutation : given.a_cookie_antiforgery_middleware
{
    void Establish()
    {
        _context.Request.Headers.Cookie = "_oauth2_proxy=session";
        _context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "alice")], TrustedProxyUsers.AuthenticationType));
        _context.Request.Method = HttpMethods.Post;
    }

    async Task Because() => await _middleware.InvokeAsync(_context, _antiforgery);

    [Fact] void should_validate_antiforgery() => _antiforgery.Received(1).ValidateRequestAsync(_context);
}
