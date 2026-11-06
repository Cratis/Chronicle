// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Namespaces.for_NamespaceCasing;

public class when_a_case_variant_hits_the_cache : given.a_namespace_casing
{
    void Establish() => _namespaces.GetCanonical("Cratis").Returns((EventStoreNamespaceName)"Cratis");

    async Task Because()
    {
        _first = await _casing.Canonicalize("Studio", "Cratis");
        _second = await _casing.Canonicalize("Studio", "cratis");
    }

    [Fact] void should_return_the_stored_casing_for_both_inputs() => _second.ShouldEqual(_first);
    [Fact]
    async Task should_only_resolve_the_first_input() => await _namespaces.Received(1).GetCanonical("Cratis");
    [Fact]
    async Task should_not_query_for_the_cached_variant() => await _namespaces.DidNotReceive().GetCanonical("cratis");
}
