// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections.for_OAuthTokenProvider;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_ExternalAuthorityTokenProvider.given;

public class an_external_authority : Specification
{
    protected const string DiscoveryUrl = "https://idp.example.com/realms/chronicle/.well-known/openid-configuration";
    protected const string TokenUrl = "https://idp.example.com/realms/chronicle/protocol/openid-connect/token";

    internal ExternalAuthorityTokenProvider _provider;
    protected CapturingTokenEndpointHandler _handler;
    protected ControllableTimeProvider _time;

    void Establish()
    {
        _handler = new CapturingTokenEndpointHandler();
        _time = new ControllableTimeProvider();
        _provider = new ExternalAuthorityTokenProvider(
            new Uri("https://idp.example.com/realms/chronicle/"),
            "test-client",
            "test-secret",
            "api://chronicle/.default",
            Substitute.For<ILogger<ExternalAuthorityTokenProvider>>(),
            Substitute.For<ILogger<OAuthTokenProvider>>(),
            _handler,
            _time);
    }

    void Destroy() => _provider.Dispose();
}
