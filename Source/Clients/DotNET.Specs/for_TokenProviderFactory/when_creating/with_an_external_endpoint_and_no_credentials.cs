// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_TokenProviderFactory.when_creating;

public class with_an_external_endpoint_and_no_credentials : given.a_factory_context
{
    Exception _result2;

    void Because() => _result2 = Catch.Exception(() => Create(Options("chronicle://localhost:35000", new() { TokenEndpoint = "https://idp.example.com/oauth2/token" })));

    [Fact] void should_throw_client_credentials_required() => _result2.ShouldBeOfExactType<ClientCredentialsRequiredForExternalTokenAuthority>();
}
