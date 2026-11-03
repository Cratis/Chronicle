// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_OAuthTokenProvider.when_requesting_a_token;

public class and_the_token_response_echoes_the_request : Specification
{
    OAuthTokenProvider _provider;
    CapturingTokenEndpointHandler _handler;
    CapturingLogger<OAuthTokenProvider> _log;

    void Establish()
    {
        _handler = new CapturingTokenEndpointHandler
        {
            TokenErrorBody = "{\"error\":\"invalid_client\",\"error_description\":\"rejected grant_type=client_credentials&client_secret=test-secret\"}"
        };
        _log = new CapturingLogger<OAuthTokenProvider>();
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

    [Fact] void should_not_log_the_response_body() => _log.Entries.Any(_ => _.Contains("test-secret")).ShouldBeFalse();
    [Fact] void should_not_log_the_error_description() => _log.Entries.Any(_ => _.Contains("rejected")).ShouldBeFalse();
    [Fact] void should_log_the_status_code() => _log.Entries.Any(_ => _.Level == LogLevel.Error && _.Message.Contains("400", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_log_the_oauth_error_code() => _log.Entries.Any(_ => _.Message.Contains("invalid_client", StringComparison.Ordinal)).ShouldBeTrue();
}
