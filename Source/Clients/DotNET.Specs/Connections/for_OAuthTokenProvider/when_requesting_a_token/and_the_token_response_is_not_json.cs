// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_OAuthTokenProvider.when_requesting_a_token;

public class and_the_token_response_is_not_json : Specification
{
    OAuthTokenProvider _provider;
    CapturingTokenEndpointHandler _handler;
    RecordingLogger<OAuthTokenProvider> _log;

    void Establish()
    {
        _handler = new CapturingTokenEndpointHandler { TokenErrorBody = "<html>client_secret=test-secret and error=custom_code</html>" };
        _log = new RecordingLogger<OAuthTokenProvider>();
        _provider = new OAuthTokenProvider(
            new Uri("https://idp.example.com/oauth2/token"),
            "test-client",
            "test-secret",
            null,
            _log,
            _handler,
            new ControllableTimeProvider());
    }

    async Task Because() => await Catch.Exception(() => _provider.GetAccessToken());

    void Destroy() => _provider.Dispose();

    [Fact] void should_not_log_any_of_the_response_body() => _log.Entries.Any(_ => _.Message.Contains("test-secret", StringComparison.Ordinal) || _.Message.Contains("custom_code", StringComparison.Ordinal)).ShouldBeFalse();
}
