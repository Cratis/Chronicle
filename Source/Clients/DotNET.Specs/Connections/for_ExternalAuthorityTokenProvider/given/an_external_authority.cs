// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections.for_OAuthTokenProvider;
namespace Cratis.Chronicle.Connections.for_ExternalAuthorityTokenProvider.given;

public class an_external_authority : Specification
{
    protected const string DiscoveryUrl = "https://idp.example.com/realms/chronicle/.well-known/openid-configuration";
    protected const string TokenUrl = "https://idp.example.com/realms/chronicle/protocol/openid-connect/token";

    internal ExternalAuthorityTokenProvider _provider;
    protected CapturingTokenEndpointHandler _handler;
    protected ControllableTimeProvider _time;
    internal RecordingLogger<ExternalAuthorityTokenProvider> _log;
    internal RecordingLogger<OAuthTokenProvider> _tokenLog;

    void Establish()
    {
        _handler = new CapturingTokenEndpointHandler();
        _time = new ControllableTimeProvider();
        _log = new RecordingLogger<ExternalAuthorityTokenProvider>();
        _tokenLog = new RecordingLogger<OAuthTokenProvider>();
        _provider = new ExternalAuthorityTokenProvider(
            new Uri("https://idp.example.com/realms/chronicle/"),
            "test-client",
            "test-secret",
            "api://chronicle/.default",
            _log,
            _tokenLog,
            _handler,
            _time);
    }

    void Destroy() => _provider.Dispose();
}
