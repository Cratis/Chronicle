// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_OAuthTokenProvider.when_requesting_a_token;

public class and_a_hostile_reason_phrase_is_returned_while_a_cached_token_is_valid : Specification
{
    OAuthTokenProvider _provider;
    CapturingTokenEndpointHandler _handler;
    CapturingLogger<OAuthTokenProvider> _log;
    string _result;

    async Task Establish()
    {
        _handler = new CapturingTokenEndpointHandler();
        _log = new CapturingLogger<OAuthTokenProvider>();
        var time = new ControllableTimeProvider();
        _provider = new OAuthTokenProvider(
            new Uri("https://idp.example.com/oauth2/token"),
            "test-client",
            "test-secret",
            null,
            _log,
            _handler,
            time);
        await _provider.GetAccessToken();
        time.Advance(TimeSpan.FromSeconds(3570));
        _handler.TokenStatus = HttpStatusCode.ServiceUnavailable;
        _handler.TokenReasonPhrase = "client_secret=leak-marker";
    }

    async Task Because() => _result = (await _provider.GetAccessToken())!;

    void Destroy() => _provider.Dispose();

    [Fact] void should_serve_the_cached_token() => _result.ShouldEqual("the-token");
    [Fact] void should_attempt_a_refresh() => _handler.TokenRequests.Count().ShouldEqual(2);
    [Fact] void should_log_the_fallback() => _log.Entries.Any(_ => _.Level == LogLevel.Warning && _.Exception is HttpRequestException).ShouldBeTrue();
    [Fact] void should_preserve_the_status_code() => ((HttpRequestException)_log.Entries.Single(_ => _.Level == LogLevel.Warning && _.Exception is HttpRequestException).Exception!).StatusCode.ShouldEqual(HttpStatusCode.ServiceUnavailable);
    [Fact] void should_not_log_the_remote_reason_phrase() => _log.Entries.Any(_ => _.Contains("leak-marker")).ShouldBeFalse();
    [Fact] void should_not_keep_an_unsafe_inner_exception() => _log.Entries.Where(_ => _.Exception is not null).All(_ => _.Exception!.InnerException is null).ShouldBeTrue();
}
