// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.for_TokenProviderFactory.when_creating;

public class with_an_insecure_token_endpoint : given.a_factory_context
{
    Exception _result2;
    Exception _authorityResult;

    void Because()
    {
        _result2 = Catch.Exception(() => Create(Options(WithCredentials, new() { TokenEndpoint = "http://idp.example.com/hostile-path-marker/token" })));
        _authorityResult = Catch.Exception(() => Create(Options(WithCredentials, new() { Authority = "http://idp.example.com/hostile-path-marker" })));
    }

    [Fact] void should_throw_insecure_token_endpoint() => _result2.ShouldBeOfExactType<InsecureTokenEndpoint>();
    [Fact] void should_throw_insecure_token_endpoint_for_an_authority() => _authorityResult.ShouldBeOfExactType<InsecureTokenEndpoint>();
    [Fact] void should_not_name_the_endpoint() => new[] { _result2, _authorityResult }.All(_ => !_.ToString().Contains("idp.example.com", StringComparison.Ordinal) && !_.ToString().Contains("hostile-path-marker", StringComparison.Ordinal)).ShouldBeTrue();
}
