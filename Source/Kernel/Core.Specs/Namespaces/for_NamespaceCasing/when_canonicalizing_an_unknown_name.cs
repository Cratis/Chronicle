// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Namespaces.for_NamespaceCasing;

public class when_canonicalizing_an_unknown_name : given.a_namespace_casing
{
    async Task Because()
    {
        _first = await _casing.Canonicalize("Studio", "unknown");
        _second = await _casing.Canonicalize("Studio", "unknown");
    }

    [Fact] void should_return_the_requested_name() => _first.Value.ShouldEqual("unknown");
    [Fact] async Task should_not_cache_an_unknown_name() => await _namespaces.Received(2).GetCanonical("unknown");
}
