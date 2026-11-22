// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnations;

public class when_a_listed_namespace_is_closing : given.incarnation_routing
{
    IEnumerable<EventStoreNamespaceName> _listed;

    void Establish() => _state = NamespaceLifecycle.Open(_namespace, NamespaceIncarnationId.New(), DateTimeOffset.UtcNow).BeginRemoval(Guid.NewGuid(), DateTimeOffset.UtcNow);

    async Task Because() => _listed = await _routing.GetAll(_eventStore);

    [Fact] void should_not_reintroduce_it_to_forwarding_subscriptions() => _listed.ShouldBeEmpty();
    [Fact] async Task should_not_recreate_it_while_reading() => await _owner.DidNotReceive().Ensure(_namespace);
}
