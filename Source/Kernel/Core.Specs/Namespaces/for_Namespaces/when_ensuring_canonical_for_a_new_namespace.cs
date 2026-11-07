// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Namespaces.for_Namespaces;

public class when_ensuring_canonical_for_a_new_namespace : given.a_namespaces_grain
{
    EventStoreNamespaceName _result;

    async Task Because() => _result = await _namespaces.EnsureCanonical("NewNamespace");

    [Fact] void should_keep_the_requested_casing() => _result.Value.ShouldEqual("NewNamespace");
    [Fact] void should_add_the_new_name() => _state.NewNamespaces.Single().Name.ShouldEqual(_result);
}
