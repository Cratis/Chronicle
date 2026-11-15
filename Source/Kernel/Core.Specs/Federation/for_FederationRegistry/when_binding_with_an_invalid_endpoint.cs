// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationRegistry;

public class when_binding_with_an_invalid_endpoint : given.a_federation_registry
{
    Exception _plainHttp;
    Exception _relative;

    async Task Because()
    {
        _plainHttp = await Catch.Exception(() => _registry.Bind(Binding(endpoint: "http://peer.example.com:35100")));
        _relative = await Catch.Exception(() => _registry.Bind(Binding(endpoint: "peer.example.com")));
    }

    [Fact] void should_refuse_plain_http() => _plainHttp.ShouldBeOfExactType<InvalidFederationBinding>();
    [Fact] void should_refuse_a_relative_uri() => _relative.ShouldBeOfExactType<InvalidFederationBinding>();
    [Fact] void should_not_append_anything() => _appended.ShouldBeEmpty();
}
