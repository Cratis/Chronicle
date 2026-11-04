// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_OAuthTokenProvider.when_requesting_a_token;

public class and_no_external_endpoint_is_configured : Specification
{
    ChronicleServerAddress _address;
    CapturingTokenEndpointHandler _handler;
    OAuthTokenProvider _provider;

    void Establish()
    {
        _address = new ChronicleServerAddress("localhost");
        _handler = new CapturingTokenEndpointHandler();
        _provider = new OAuthTokenProvider(
            () => _address,
            "test-client",
            "test-secret",
            Substitute.For<ILogger<OAuthTokenProvider>>(),
            _handler,
            new ControllableTimeProvider());
    }

    async Task Because() => await _provider.GetAccessToken();

    void Destroy() => _provider.Dispose();

    [Fact] void should_post_to_the_server_token_endpoint() => _handler.Requests.Single().Uri.AbsoluteUri.ShouldEqual(new Uri($"https://{_address.Host}:{_address.Port}/connect/token").AbsoluteUri);
    [Fact] void should_send_exactly_the_default_fields() => _handler.Requests.Single().Form.Keys.Order().ShouldContainOnly("client_id", "client_secret", "grant_type");
    [Fact] void should_not_send_a_scope() => _handler.Requests.Single().Form.ContainsKey("scope").ShouldBeFalse();
}
