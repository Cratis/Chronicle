// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.for_TokenProviderFactory.when_creating;

public class with_a_token_endpoint : given.a_factory_context
{
    void Because() => _result = Create(Options(WithCredentials, new() { TokenEndpoint = "https://idp.example.com/oauth2/token" }));

    [Fact] void should_create_an_oauth_token_provider() => _result.ShouldBeOfExactType<OAuthTokenProvider>();
    [Fact] void should_use_the_configured_endpoint() => ((OAuthTokenProvider)_result).GetCurrentTokenEndpoint().ShouldEqual("https://idp.example.com/oauth2/token");
}
