// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Namespaces.for_NamespaceCasing;

public class when_forgetting_during_a_resolution : given.a_namespace_casing
{
    readonly TaskCompletionSource<EventStoreNamespaceName?> _pending = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish() => _namespaces.GetCanonical("cratis").Returns(_pending.Task);

    async Task Because()
    {
        var resolving = _casing.Canonicalize("Studio", "cratis");
        _casing.Forget("Studio", "Cratis");
        _pending.SetResult("Cratis");
        _first = await resolving;
        _namespaces.GetCanonical("cratis").Returns((EventStoreNamespaceName)"cratis");
        _second = await _casing.Canonicalize("Studio", "cratis");
    }

    [Fact] void should_not_repopulate_the_cache_with_the_removed_casing() => _second.Value.ShouldEqual("cratis");
    [Fact] async Task should_resolve_again() => await _namespaces.Received(2).GetCanonical("cratis");
}
