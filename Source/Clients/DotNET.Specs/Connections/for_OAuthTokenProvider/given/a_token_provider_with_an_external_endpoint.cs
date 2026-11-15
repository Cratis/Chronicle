// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_OAuthTokenProvider.given;

public class a_token_provider_with_an_external_endpoint : Specification
{
    protected static readonly Uri _endpoint = new("https://idp.example.com/oauth2/token");

    protected OAuthTokenProvider _provider;
    protected CapturingTokenEndpointHandler _handler;
    protected ControllableTimeProvider _time;

    void Establish()
    {
        _handler = new CapturingTokenEndpointHandler();
        _time = new ControllableTimeProvider();
        _provider = new OAuthTokenProvider(
            _endpoint,
            "test-client",
            "test-secret",
            "api://chronicle/.default",
            Substitute.For<ILogger<OAuthTokenProvider>>(),
            _handler,
            _time);
    }

    void Destroy() => _provider.Dispose();
}
