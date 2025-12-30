// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_OAuthTokenProvider.given;

public class an_oauth_token_provider : Specification
{
    protected OAuthTokenProvider _provider;
    protected ILogger<OAuthTokenProvider> _logger;
    protected ChronicleServerAddress _serverAddress;
    protected string _clientId;
    protected string _clientSecret;
    protected int _managementPort;
    protected bool _disableTls;

    void Establish()
    {
        _logger = Substitute.For<ILogger<OAuthTokenProvider>>();
        _serverAddress = new ChronicleServerAddress("localhost");
        _clientId = "test-client";
        _clientSecret = "test-secret";
        _managementPort = 8080;
        _disableTls = true;

        _provider = new OAuthTokenProvider(
            _serverAddress,
            _clientId,
            _clientSecret,
            _managementPort,
            _disableTls,
            _logger);
    }
}
