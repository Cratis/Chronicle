// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.for_TokenProviderFactory.when_creating;

public class with_an_insecure_token_endpoint : given.a_factory_context
{
    Exception _result2;

    void Because() => _result2 = Catch.Exception(() => Create(Options(WithCredentials, new() { TokenEndpoint = "http://idp.example.com/oauth2/token" })));

    [Fact] void should_throw_insecure_token_endpoint() => _result2.ShouldBeOfExactType<InsecureTokenEndpoint>();
}
