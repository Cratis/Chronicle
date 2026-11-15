// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Namespaces.for_NamespaceCasing;

public class when_canonicalizing_a_default_variant : given.a_namespace_casing
{
    async Task Because() => _first = await _casing.Canonicalize("Studio", "default");

    [Fact] void should_return_the_reserved_casing() => _first.ShouldEqual(EventStoreNamespaceName.Default);
    [Fact] async Task should_not_query_the_grain() => await _namespaces.DidNotReceiveWithAnyArgs().GetCanonical(default!);
}
