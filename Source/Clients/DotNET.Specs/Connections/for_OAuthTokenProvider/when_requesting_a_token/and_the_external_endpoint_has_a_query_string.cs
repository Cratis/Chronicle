// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_OAuthTokenProvider.when_requesting_a_token;

public class and_the_external_endpoint_has_a_query_string : Specification
{
    OAuthTokenProvider _provider;
    CapturingTokenEndpointHandler _handler;
    CapturingLogger<OAuthTokenProvider> _log;

    void Establish()
    {
        _handler = new CapturingTokenEndpointHandler { TokenErrorBody = "{\"error\":\"invalid_client\"}" };
        _log = new CapturingLogger<OAuthTokenProvider>();
        _provider = new OAuthTokenProvider(
            new Uri("https://idp.example.com/oauth2/token?tenant=hidden-tenant-value"),
            "test-client",
            "test-secret",
            null,
            _log,
            _handler,
            new ControllableTimeProvider());
    }

    async Task Because() => await Catch.Exception(() => _provider.GetAccessToken());

    void Destroy() => _provider.Dispose();

    [Fact] void should_send_the_request_to_the_full_endpoint() => _handler.Requests.Single().Uri.Query.ShouldEqual("?tenant=hidden-tenant-value");
    [Fact] void should_log_the_endpoint() => _log.Entries.Any(_ => _.Message.Contains("https://idp.example.com/oauth2/token", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_log_the_query_string() => _log.Entries.Any(_ => _.Contains("hidden-tenant-value")).ShouldBeFalse();
}
