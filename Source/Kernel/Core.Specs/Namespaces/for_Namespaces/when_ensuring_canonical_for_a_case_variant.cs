// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_Namespaces;

public class when_ensuring_canonical_for_a_case_variant : given.a_namespaces_grain
{
    EventStoreNamespaceName _result;

    void Establish() => _state.Namespaces.Add(new NamespaceState("Cratis", DateTimeOffset.UtcNow));

    async Task Because() => _result = await _namespaces.EnsureCanonical("cratis");

    [Fact] void should_return_the_stored_casing() => _result.Value.ShouldEqual("Cratis");
    [Fact] async Task should_not_append_an_event() => await _systemSequence.DidNotReceive().Append(Arg.Any<EventSourceId>(), Arg.Any<object>());
    [Fact] async Task should_not_broadcast() => await _writer.DidNotReceive().Publish(Arg.Any<NamespaceAdded>());
    [Fact] void should_not_add_a_namespace() => _state.NewNamespaces.ShouldBeEmpty();
}
