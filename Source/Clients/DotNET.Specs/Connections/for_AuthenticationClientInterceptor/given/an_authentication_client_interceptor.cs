// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_AuthenticationClientInterceptor.given;

public class an_authentication_client_interceptor : Specification
{
    protected AuthenticationClientInterceptor _interceptor;
    protected ITokenProvider _tokenProvider;
    protected ILogger<AuthenticationClientInterceptor> _logger;

    void Establish()
    {
        _tokenProvider = Substitute.For<ITokenProvider>();
        _logger = Substitute.For<ILogger<AuthenticationClientInterceptor>>();
        _interceptor = new AuthenticationClientInterceptor(_tokenProvider, _logger);
    }
}
