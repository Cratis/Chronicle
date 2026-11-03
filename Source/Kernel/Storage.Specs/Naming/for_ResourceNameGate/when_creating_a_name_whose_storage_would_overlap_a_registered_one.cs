// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameGate;

public class when_creating_a_name_whose_storage_would_overlap_a_registered_one : given.a_gate
{
    ResourceNameConflict _store;
    ResourceNameConflict _namespaceInTheSameStore;
    ResourceNameConflict _namespaceOfAnotherStore;
    ResourceNameConflict _namespaceOfALegacyStore;
    Exception _unrelated;

    void Because()
    {
        _gate.MarkRegistered(new EventStoreName("a+es"));
        _store = (ResourceNameConflict)Catch.Exception(() => _gate.EnsureCanCreate(Store("x", ["X+es"])).GetAwaiter().GetResult());
        _namespaceInTheSameStore = (ResourceNameConflict)Catch.Exception(() => _gate.EnsureCanCreate(Namespace("a", "b", ["es+b"])).GetAwaiter().GetResult());
        _namespaceOfAnotherStore = (ResourceNameConflict)Catch.Exception(() => _gate.EnsureCanCreate(Namespace("a", "b", [], ["a", "a+es"], OtherStoreHasNamespace)).GetAwaiter().GetResult());
        _namespaceOfALegacyStore = (ResourceNameConflict)Catch.Exception(() => _gate.EnsureCanCreate(Namespace("a+es", "b", [], ["a+es", "a"], OtherStoreHasNamespace)).GetAwaiter().GetResult());
        _unrelated = Catch.Exception(() => _gate.EnsureCanCreate(Namespace("a", "c", ["b"], ["a", "a+es"], OtherStoreHasNamespace)).GetAwaiter().GetResult());
    }

    [Fact] void should_refuse_a_store_that_is_the_read_model_database_of_a_legacy_store() => _store.Kind.ShouldEqual(ResourceNameConflictKind.Overlap);
    [Fact] void should_refuse_a_namespace_overlapping_a_legacy_namespace_of_the_same_store() => _namespaceInTheSameStore.Kind.ShouldEqual(ResourceNameConflictKind.Overlap);
    [Fact] void should_refuse_a_namespace_overlapping_a_namespace_of_another_store() => _namespaceOfAnotherStore.Kind.ShouldEqual(ResourceNameConflictKind.Overlap);
    [Fact] void should_refuse_a_namespace_of_a_legacy_store_overlapping_another_store() => _namespaceOfALegacyStore.Kind.ShouldEqual(ResourceNameConflictKind.Overlap);
    [Fact] void should_allow_an_unrelated_namespace() => _unrelated.ShouldBeNull();
    [Fact] void should_use_the_overlap_message() => _store.Message.ShouldEqual(ResourceNames.OverlapMessage);
    [Fact] void should_only_reserve_the_name_that_was_allowed() => _reservations.Received(1).Reserve(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>());

    static Task<IEnumerable<string>> OtherStoreHasNamespace(string eventStore) => Task.FromResult<IEnumerable<string>>(["b"]);
}
