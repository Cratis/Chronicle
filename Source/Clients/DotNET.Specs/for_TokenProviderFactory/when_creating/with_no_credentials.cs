// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.for_TokenProviderFactory.when_creating;

public class with_no_credentials : given.a_factory_context
{
    void Because() => _result = Create(Options("chronicle://localhost:35000"));

    [Fact] void should_create_an_oauth_token_provider() => _result.ShouldBeOfExactType<OAuthTokenProvider>();
    [Fact] void should_use_the_server_token_endpoint() => ((OAuthTokenProvider)_result).GetCurrentTokenEndpoint().ShouldEqual("https://localhost:35000/connect/token");
}
