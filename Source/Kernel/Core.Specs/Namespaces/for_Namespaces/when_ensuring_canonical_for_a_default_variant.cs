// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Namespaces.for_Namespaces;

public class when_ensuring_canonical_for_a_default_variant : given.a_namespaces_grain
{
    EventStoreNamespaceName _result;

    async Task Because() => _result = await _namespaces.EnsureCanonical("default");

    [Fact] void should_use_the_reserved_casing() => _result.ShouldEqual(EventStoreNamespaceName.Default);
    [Fact] void should_create_the_reserved_casing() => _state.NewNamespaces.Single().Name.ShouldEqual(EventStoreNamespaceName.Default);
}
