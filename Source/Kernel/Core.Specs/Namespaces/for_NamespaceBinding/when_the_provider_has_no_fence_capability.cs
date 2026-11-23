// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Namespaces.for_NamespaceBinding;

public class when_the_provider_has_no_fence_capability : given.a_binding_lookup
{
    Exception _error;

    void Establish() => _storage.GetEventStore(_eventStore).Returns(Substitute.For<IEventStoreStorage>());

    async Task Because() => _error = await Catch.Exception(Lookup);

    [Fact] void should_refuse_binding() => _error.ShouldBeOfExactType<NamespaceIncarnationUnavailable>();
    [Fact] void should_not_resolve_a_namespace() => _names.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_activate_or_recreate_a_namespace() => _grains.ReceivedCalls().ShouldBeEmpty();
}
