// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.Server.Authentication.for_CookieAntiforgeryMiddleware;

public class when_handling_a_bearer_mutation : given.a_cookie_antiforgery_middleware
{
    void Establish()
    {
        _context.Request.Method = HttpMethods.Post;
        _context.Request.Headers.Cookie = string.Empty;
    }

    async Task Because() => await _middleware.InvokeAsync(_context, _antiforgery);

    [Fact] void should_not_validate_an_antiforgery_token() => _antiforgery.DidNotReceive().ValidateRequestAsync(_context);
    [Fact] void should_continue() => _next.Received(1).Invoke(_context);
}
