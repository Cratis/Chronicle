// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Namespaces.for_NamespaceBindings;

public class when_an_open_identity_is_empty : given.a_namespace_lookup
{
    void Establish() => _response.Incarnation = Guid.Empty;
    Task Because() => Lookup();

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<NamespaceBindingsNotSupported>();
    [Fact] void should_not_return_a_binding() => _binding.ShouldBeNull();
}
