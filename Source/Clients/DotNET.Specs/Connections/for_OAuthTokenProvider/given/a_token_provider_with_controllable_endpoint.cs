// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_OAuthTokenProvider.given;

public class a_token_provider_with_controllable_endpoint : Specification
{
    protected OAuthTokenProvider _provider;
    protected RecordingLogger<OAuthTokenProvider> _logger;
    protected FakeTokenEndpointHandler _endpoint;
    protected ControllableTimeProvider _time;

    void Establish()
    {
        _logger = new RecordingLogger<OAuthTokenProvider>();
        _endpoint = new FakeTokenEndpointHandler();
        _time = new ControllableTimeProvider();

        _provider = new OAuthTokenProvider(
            () => new ChronicleServerAddress("localhost"),
            "test-client",
            "test-secret",
            _logger,
            _endpoint,
            _time);
    }
}
