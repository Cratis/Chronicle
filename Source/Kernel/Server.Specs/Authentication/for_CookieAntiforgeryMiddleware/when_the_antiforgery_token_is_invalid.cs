// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.Server.Authentication.for_CookieAntiforgeryMiddleware;

public class when_the_antiforgery_token_is_invalid : given.a_cookie_antiforgery_middleware
{
    void Establish()
    {
        _context.Request.Method = HttpMethods.Post;
        _antiforgery.ValidateRequestAsync(_context)
            .Returns(Task.FromException(new AntiforgeryValidationException("invalid")));
    }

    async Task Because() => await _middleware.InvokeAsync(_context, _antiforgery);

    [Fact] void should_return_bad_request() => _context.Response.StatusCode.ShouldEqual(StatusCodes.Status400BadRequest);
    [Fact] void should_not_continue() => _next.DidNotReceive().Invoke(_context);
}
