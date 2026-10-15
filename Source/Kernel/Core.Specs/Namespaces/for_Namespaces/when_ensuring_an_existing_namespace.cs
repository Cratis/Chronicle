// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_Namespaces;

public class when_ensuring_an_existing_namespace : given.a_namespaces_grain
{
    void Establish() => _state.Namespaces.Add(new NamespaceState("FUTURE", DateTimeOffset.UtcNow));

    async Task Because() => await _namespaces.Ensure(_namespace);

    [Fact] async Task should_not_append_another_namespace_added_event() =>
        await _systemSequence.DidNotReceive().Append(Arg.Any<EventSourceId>(), Arg.Any<object>());
    [Fact] async Task should_not_broadcast_another_namespace_added_event() =>
        await _writer.DidNotReceive().Publish(Arg.Any<NamespaceAdded>());
    [Fact] void should_not_add_another_namespace() => _state.NewNamespaces.ShouldBeEmpty();
}
