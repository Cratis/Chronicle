// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.for_TokenProviderFactory.when_creating;

public class with_an_api_key : given.a_factory_context
{
    void Because() => _result = Create(Options("chronicle://localhost:35000?apiKey=k", new() { TokenEndpoint = "https://idp.example.com/oauth2/token" }));

    [Fact] void should_create_a_no_op_token_provider() => _result.ShouldBeOfExactType<NoOpTokenProvider>();
}
