// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.InMemory.Sinks.Graphs;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemoryGraphSinkFactory;

public class when_binding_provenance : Specification
{
    SinkProvenanceState _initial;
    SinkProvenanceState _sameScope;
    SinkProvenanceState _otherNamespace;
    SinkProvenanceState _document;
    Exception? _incompleteAdmission;

    async Task Because()
    {
        await using var cache = new SinkProvenanceCache();
        ISinkFactory graph = new InMemoryGraphSinkFactory(cache);
        ISinkFactory anotherGraphFactory = new InMemoryGraphSinkFactory(cache);
        ISinkFactory document = new InMemorySinkFactory(Substitute.For<ITypeFormats>(), cache);
        var bound = graph.GetProvenance("store", "scope", SinkName.Default, "first");
        _initial = await bound.ReadState();
        _incompleteAdmission = await Catch.Exception(async () => await (await bound.GetLease()).Ensure());
        await bound.CompleteInventory(false);
        await bound.MarkQuarantined();
        _sameScope = await anotherGraphFactory.GetProvenance("store", "scope", SinkName.Default, "second").ReadState();
        _otherNamespace = await graph.GetProvenance("store", "other", SinkName.Default).ReadState();
        _document = await document.GetProvenance("store", "scope", SinkName.Default).ReadState();
    }

    [Fact] void should_start_incomplete() => _initial.IsComplete.ShouldBeFalse();
    [Fact] void should_deny_until_inventory_completes() => _incompleteAdmission.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_share_identity_across_models() => _sameScope.DatabaseId.ShouldEqual(_initial.DatabaseId);
    [Fact] void should_keep_a_durable_scope_mark() => _sameScope.Restricted.ShouldBeTrue();
    [Fact] void should_isolate_other_namespaces() => _otherNamespace.DatabaseId.ShouldNotEqual(_initial.DatabaseId);
    [Fact] void should_isolate_document_storage() => _document.DatabaseId.ShouldNotEqual(_initial.DatabaseId);
}
