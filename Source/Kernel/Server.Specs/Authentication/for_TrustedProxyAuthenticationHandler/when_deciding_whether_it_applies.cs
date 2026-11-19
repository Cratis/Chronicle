// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.Server.Authentication.for_TrustedProxyAuthenticationHandler;

public class when_deciding_whether_it_applies : Specification
{
    static readonly Configuration.Authentication _proxied = new() { Http = new() { Mode = HttpAuthenticationMode.TrustedProxy } };

    [Fact] void should_apply_to_an_http_request_with_the_user_header() => TrustedProxyAuthenticationHandler.Applies(Request(), _proxied).ShouldBeTrue();
    [Fact] void should_not_apply_without_the_user_header() => TrustedProxyAuthenticationHandler.Applies(Request(user: null), _proxied).ShouldBeFalse();
    [Fact] void should_never_apply_to_a_grpc_call() => TrustedProxyAuthenticationHandler.Applies(Request(contentType: "application/grpc+proto"), _proxied).ShouldBeFalse();
    [Fact] void should_not_apply_when_the_mode_is_off() => TrustedProxyAuthenticationHandler.Applies(Request(), new()).ShouldBeFalse();

    static HttpRequest Request(string? user = "alice", string? contentType = null)
    {
        var context = new DefaultHttpContext();
        if (user is not null) context.Request.Headers["X-Forwarded-User"] = user;
        context.Request.ContentType = contentType;
        return context.Request;
    }
}
