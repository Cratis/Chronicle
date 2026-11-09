// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Namespaces.for_NamespaceCasing;

public class when_forgetting_one_namespace_in_one_store : given.a_namespace_casing
{
    void Establish()
    {
        _namespaces.GetCanonical("cratis").Returns((EventStoreNamespaceName)"Cratis");
        _namespaces.GetCanonical("other").Returns((EventStoreNamespaceName)"Other");
    }

    async Task Because()
    {
        await _casing.Canonicalize("Studio", "cratis");
        await _casing.Canonicalize("Studio", "other");
        await _casing.Canonicalize("Another", "cratis");
        _casing.Forget("Studio", "CRATIS");
        _first = await _casing.Canonicalize("Studio", "other");
        _second = await _casing.Canonicalize("Another", "cratis");
    }

    [Fact] void should_keep_the_other_namespace_cached() => _first.Value.ShouldEqual("Other");
    [Fact] void should_keep_the_other_store_cached() => _second.Value.ShouldEqual("Cratis");
    [Fact] async Task should_not_resolve_the_other_namespace_again() => await _namespaces.Received(1).GetCanonical("other");
    [Fact] async Task should_not_resolve_the_other_store_again() => await _namespaces.Received(2).GetCanonical("cratis");
}
