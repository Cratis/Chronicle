// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Primitives;

namespace Cratis.Chronicle.Server.Authentication.for_TrustedProxyAuthenticationHandler;

public class when_the_user_header_is_repeated : given.a_trusted_proxy_handler
{
    void Establish() => _context.Request.Headers["X-Forwarded-User"] = new StringValues(["mallory", "alice"]);

    Task Because() => Authenticate();

    [Fact] void should_not_authenticate() => _result.Succeeded.ShouldBeFalse();
}
