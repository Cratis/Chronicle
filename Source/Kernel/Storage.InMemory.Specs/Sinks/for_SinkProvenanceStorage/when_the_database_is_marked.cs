// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_SinkProvenanceStorage;

public class when_the_database_is_marked : Specification
{
    bool _sameScope;
    bool _newScope;
    bool _otherNamespace;
    bool _otherStore;

    async Task Because()
    {
        var factory = new InMemorySinkFactory(new TypeFormats());
        var marker = factory.GetProvenance("store", "namespace", SinkName.Default);
        var newScope = new InMemorySinkFactory(new TypeFormats()).GetProvenance("store", "namespace", SinkName.Default);
        var otherNamespace = factory.GetProvenance("store", "other-namespace", SinkName.Default);
        var otherStore = factory.GetProvenance("other-store", "namespace", SinkName.Default);
        await Task.WhenAll(newScope.CompleteInventory(false), otherNamespace.CompleteInventory(false), otherStore.CompleteInventory(false));

        await Task.WhenAll(marker.MarkQuarantined(), marker.MarkQuarantined());
        _sameScope = await factory.GetProvenance("store", "namespace", SinkName.Default).IsQuarantined();
        _newScope = await newScope.IsQuarantined();
        _otherNamespace = await otherNamespace.IsQuarantined();
        _otherStore = await otherStore.IsQuarantined();
    }

    [Fact] void should_share_the_monotonic_mark_inside_the_storage_scope() => _sameScope.ShouldBeTrue();
    [Fact] void should_not_leak_a_mark_to_a_new_storage_scope() => _newScope.ShouldBeFalse();
    [Fact] void should_not_affect_an_independent_namespace() => _otherNamespace.ShouldBeFalse();
    [Fact] void should_not_affect_an_independent_store() => _otherStore.ShouldBeFalse();
}
