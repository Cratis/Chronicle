// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_OAuthTokenProvider.when_requesting_a_token;

public class and_the_external_token_endpoint_redirects : given.a_token_provider_with_an_external_endpoint
{
    Exception _result;
    CapturingLogger<OAuthTokenProvider> _log;
    OAuthTokenProvider _recordingProvider;

    void Establish()
    {
        _handler.TokenStatus = HttpStatusCode.PermanentRedirect;
        _handler.TokenReasonPhrase = "client_secret=leak-marker";
        _log = new CapturingLogger<OAuthTokenProvider>();
        _recordingProvider = new OAuthTokenProvider(_endpoint, "test-client", "test-secret", null, _log, _handler, _time);
    }

    async Task Because() => _result = await Catch.Exception(() => _recordingProvider.GetAccessToken());

    [Fact] void should_fail() => _result.ShouldBeOfExactType<HttpRequestException>();
    [Fact] void should_preserve_the_status_code() => ((HttpRequestException)_result).StatusCode.ShouldEqual(HttpStatusCode.PermanentRedirect);
    [Fact] void should_not_log_the_remote_reason_phrase() => _log.Entries.Any(_ => _.Contains("leak-marker")).ShouldBeFalse();
    [Fact] void should_not_retain_the_remote_reason_phrase() => _result.ToString().Contains("leak-marker", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_post_only_once() => _handler.Requests.Count.ShouldEqual(1);
    [Fact] void should_name_the_endpoint_in_the_log() => _log.Entries.Any(_ => _.Level == LogLevel.Error && _.Message.Contains("https://idp.example.com/oauth2/token", StringComparison.Ordinal)).ShouldBeTrue();
}
