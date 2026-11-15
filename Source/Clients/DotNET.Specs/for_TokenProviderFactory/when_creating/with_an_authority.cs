// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.for_TokenProviderFactory.when_creating;

public class with_an_authority : given.a_factory_context
{
    void Because() => _result = Create(Options(WithCredentials, new() { Authority = "https://idp.example.com/realms/chronicle" }));

    [Fact] void should_create_an_external_authority_token_provider() => _result.ShouldBeOfExactType<ExternalAuthorityTokenProvider>();
}
