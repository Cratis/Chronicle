// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.Server.Authentication.for_CookieAntiforgeryMiddleware;

public class when_bearer_credentials_accompany_an_ambient_cookie : given.a_cookie_antiforgery_middleware
{
    void Establish()
    {
        _context.Request.Method = HttpMethods.Post;
        _context.Request.Headers.Authorization = "Bearer explicit-credentials";
    }

    async Task Because() => await _middleware.InvokeAsync(_context, _antiforgery);

    [Fact] void should_not_validate_cookie_antiforgery() => _antiforgery.DidNotReceive().ValidateRequestAsync(_context);
    [Fact] void should_continue() => _next.Received(1).Invoke(_context);
}
