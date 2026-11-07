// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Namespaces.for_Namespaces;

public class when_getting_canonical_for_an_unknown_namespace : given.a_namespaces_grain
{
    EventStoreNamespaceName? _result;

    async Task Because() => _result = await _namespaces.GetCanonical("unknown");

    [Fact] void should_not_resolve_an_unknown_name() => _result.ShouldBeNull();
    [Fact] void should_not_create_a_namespace() => _state.NewNamespaces.ShouldBeEmpty();
}
