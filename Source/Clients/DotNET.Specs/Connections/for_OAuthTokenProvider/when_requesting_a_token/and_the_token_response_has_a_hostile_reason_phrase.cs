// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_OAuthTokenProvider.when_requesting_a_token;

public class and_the_token_response_has_a_hostile_reason_phrase : Specification
{
    OAuthTokenProvider _provider;
    CapturingLogger<OAuthTokenProvider> _log;
    Exception _result;

    void Establish()
    {
        _log = new CapturingLogger<OAuthTokenProvider>();
        var handler = new CapturingTokenEndpointHandler
        {
            TokenStatus = HttpStatusCode.BadRequest,
            TokenReasonPhrase = "client_secret=leak-marker",
            TokenErrorBody = "{\"error\":\"invalid_client\"}"
        };
        _provider = new OAuthTokenProvider(
            new Uri("https://idp.example.com/oauth2/token"),
            "test-client",
            "test-secret",
            null,
            _log,
            handler,
            new ControllableTimeProvider());
    }

    async Task Because() => _result = await Catch.Exception(() => _provider.GetAccessToken());

    void Destroy() => _provider.Dispose();

    [Fact] void should_fail() => _result.ShouldBeOfExactType<HttpRequestException>();
    [Fact] void should_preserve_the_status_code() => ((HttpRequestException)_result).StatusCode.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_not_keep_an_unsafe_inner_exception() => _result.InnerException.ShouldBeNull();
    [Fact] void should_log_the_failure() => _log.Entries.Any(_ => _.Level == LogLevel.Error && _.Message.Contains("400", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_log_any_exception_with_the_remote_reason_phrase() => _log.Entries.Any(_ => _.Exception?.ToString().Contains("leak-marker", StringComparison.Ordinal) == true).ShouldBeFalse();
    [Fact] void should_not_log_the_remote_reason_phrase() => _log.Entries.Any(_ => _.Contains("leak-marker")).ShouldBeFalse();
    [Fact] void should_not_retain_the_remote_reason_phrase() => _result.ToString().Contains("leak-marker", StringComparison.Ordinal).ShouldBeFalse();
}
