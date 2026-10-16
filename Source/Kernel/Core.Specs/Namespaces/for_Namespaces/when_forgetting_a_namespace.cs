// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_Namespaces;

public class when_forgetting_a_namespace : given.a_namespaces_grain
{
    void Establish() => _state.Namespaces.Add(new NamespaceState(_namespace, DateTimeOffset.UtcNow));

    async Task Because() => await _namespaces.Forget(_namespace);

    [Fact] void should_no_longer_list_the_namespace() => _state.Namespaces.ShouldBeEmpty();
    [Fact] async Task should_record_that_the_namespace_was_removed() =>
        await _systemSequence.Received(1).Append(Arg.Any<EventSourceId>(), Arg.Is<object>(_ => _ is NamespaceRemoved));
}
