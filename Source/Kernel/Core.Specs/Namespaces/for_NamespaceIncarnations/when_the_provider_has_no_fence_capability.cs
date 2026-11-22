// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnations;

public class when_the_provider_has_no_fence_capability : given.incarnation_routing
{
    Exception _error;

    void Establish() => _storage.GetEventStore(_eventStore).Returns(Substitute.For<IEventStoreStorage>());

    async Task Because() => _error = await Catch.Exception(() => _routing.Ensure(_eventStore, _namespace));

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<NamespaceIncarnationUnavailable>();
    [Fact] async Task should_never_fall_back_to_an_unfenced_owner() => await _owner.DidNotReceive().Ensure(_namespace);
}
