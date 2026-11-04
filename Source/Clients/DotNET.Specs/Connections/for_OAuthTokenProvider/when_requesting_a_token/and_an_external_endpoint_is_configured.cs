// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_OAuthTokenProvider.when_requesting_a_token;

public class and_an_external_endpoint_is_configured : given.a_token_provider_with_an_external_endpoint
{
    string _token;

    async Task Because() => _token = (await _provider.GetAccessToken())!;

    [Fact] void should_post_once() => _handler.Requests.Count.ShouldEqual(1);
    [Fact] void should_post_to_the_external_endpoint() => _handler.Requests[0].Uri.ShouldEqual(_endpoint);
    [Fact] void should_use_post() => _handler.Requests[0].Method.ShouldEqual(HttpMethod.Post);
    [Fact] void should_send_the_grant_type() => _handler.Requests[0].Form["grant_type"].ShouldEqual("client_credentials");
    [Fact] void should_send_the_client_id() => _handler.Requests[0].Form["client_id"].ShouldEqual("test-client");
    [Fact] void should_send_the_client_secret() => _handler.Requests[0].Form["client_secret"].ShouldEqual("test-secret");
    [Fact] void should_send_the_scope() => _handler.Requests[0].Form["scope"].ShouldEqual("api://chronicle/.default");
    [Fact] void should_return_the_token() => _token.ShouldEqual("the-token");
}
