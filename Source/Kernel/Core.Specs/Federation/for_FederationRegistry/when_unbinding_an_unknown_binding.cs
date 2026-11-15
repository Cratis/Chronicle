// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationRegistry;

public class when_unbinding_an_unknown_binding : given.a_federation_registry
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _registry.Unbind("nothing"));

    [Fact] void should_fail_with_binding_not_found() => _error.ShouldBeOfExactType<FederationBindingNotFound>();
    [Fact] void should_not_append_anything() => _appended.ShouldBeEmpty();
}
