// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameGate;

public class when_creating_a_case_variant : given.a_gate
{
    ResourceNameConflict _eventStore;
    ResourceNameConflict _namespace;

    void Because()
    {
        _eventStore = (ResourceNameConflict)Catch.Exception(() => _gate.EnsureCanCreate(Store("orders", ["Orders", "other"])).GetAwaiter().GetResult());
        _namespace = (ResourceNameConflict)Catch.Exception(() => _gate.EnsureCanCreate(Namespace("orders", "tenant", ["Tenant"])).GetAwaiter().GetResult());
    }

    [Fact] void should_refuse_the_event_store() => _eventStore.Kind.ShouldEqual(ResourceNameConflictKind.EventStoreCaseVariant);
    [Fact] void should_refuse_the_namespace() => _namespace.Kind.ShouldEqual(ResourceNameConflictKind.NamespaceCaseVariant);
    [Fact] void should_use_the_event_store_message() => _eventStore.Message.ShouldEqual(ResourceNames.EventStoreCaseVariantMessage);
    [Fact] void should_use_the_namespace_message() => _namespace.Message.ShouldEqual(ResourceNames.NamespaceCaseVariantMessage);
    [Fact] void should_not_reserve_anything() => _reservations.DidNotReceive().Reserve(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>());
}
