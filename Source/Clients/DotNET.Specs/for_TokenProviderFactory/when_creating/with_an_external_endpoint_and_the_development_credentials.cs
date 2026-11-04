// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_TokenProviderFactory.when_creating;

public class with_an_external_endpoint_and_the_development_credentials : given.a_factory_context
{
    Exception _result2;

    void Because() => _result2 = Catch.Exception(() => Create(new ChronicleOptions { Authentication = new() { Authority = "https://idp.example.com/realms/chronicle" } }));

    [Fact] void should_throw_client_credentials_required() => _result2.ShouldBeOfExactType<ClientCredentialsRequiredForExternalTokenAuthority>();
}
