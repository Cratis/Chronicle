// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Connections.for_OAuthTokenProvider;

public class when_creating_the_default_server_handler : Specification
{
    OAuthTokenProvider _provider;
    SocketsHttpHandler _handler;

    void Because()
    {
        _provider = new OAuthTokenProvider(
            () => new ChronicleServerAddress("localhost"),
            "test-client",
            "test-secret",
            skipTlsValidation: false,
            NullLogger<OAuthTokenProvider>.Instance);
        _handler = (SocketsHttpHandler)_provider.GetMessageHandler();
    }

    void Destroy() => _provider.Dispose();

    [Fact] void should_not_forward_credentials_by_following_redirects() => _handler.AllowAutoRedirect.ShouldBeFalse();
}
