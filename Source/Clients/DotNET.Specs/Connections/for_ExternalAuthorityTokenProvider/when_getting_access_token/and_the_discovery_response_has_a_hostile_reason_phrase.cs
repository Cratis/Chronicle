// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_ExternalAuthorityTokenProvider.when_getting_access_token;

public class and_the_discovery_response_has_a_hostile_reason_phrase : given.an_external_authority
{
    Exception _first;
    Exception _second;

    void Establish()
    {
        _handler.DiscoveryStatus = HttpStatusCode.BadRequest;
        _handler.DiscoveryReasonPhrase = "client_secret=leak-marker";
    }

    async Task Because()
    {
        _first = await Catch.Exception(() => _provider.GetAccessToken());
        _second = await Catch.Exception(() => _provider.GetAccessToken());
    }

    [Fact] void should_fail() => _first.ShouldBeOfExactType<TokenEndpointDiscoveryFailed>();
    [Fact] void should_keep_the_http_failure() => _first.InnerException.ShouldBeOfExactType<HttpRequestException>();
    [Fact] void should_preserve_the_status_code() => ((HttpRequestException)_first.InnerException!).StatusCode.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_not_keep_an_unsafe_inner_exception() => _first.InnerException!.InnerException.ShouldBeNull();
    [Fact] void should_log_the_failure() => _log.Entries.Any(_ => _.Level == LogLevel.Warning && _.Exception is HttpRequestException).ShouldBeTrue();
    [Fact] void should_not_log_the_remote_reason_phrase() => _log.Entries.Any(_ => _.Contains("leak-marker")).ShouldBeFalse();
    [Fact] void should_not_retain_the_remote_reason_phrase() => _first.ToString().Contains("leak-marker", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_retain_the_remote_reason_phrase_in_the_throttled_failure() => _second.ToString().Contains("leak-marker", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_only_discover_once() => _handler.DiscoveryRequests.Count().ShouldEqual(1);
    [Fact] void should_not_request_a_token() => _handler.TokenRequests.ShouldBeEmpty();
}
