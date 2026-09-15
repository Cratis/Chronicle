// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.Server.Authentication.for_CookieAntiforgeryMiddleware;

public class when_handling_an_authenticated_cookie_mutation : given.a_cookie_antiforgery_middleware
{
    void Establish() => _context.Request.Method = HttpMethods.Post;

    async Task Because() => await _middleware.InvokeAsync(_context, _antiforgery);

    [Fact] void should_validate_the_request_token() => _antiforgery.Received(1).ValidateRequestAsync(_context);
    [Fact] void should_continue() => _next.Received(1).Invoke(_context);
}
