// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_Namespaces;

public class when_forgetting_a_case_variant_of_a_namespace : given.a_namespaces_grain
{
    void Establish() => _state.Namespaces.Add(new NamespaceState("orders", DateTimeOffset.UtcNow));

    async Task Because() => await _namespaces.Forget("Orders");

    [Fact] void should_leave_the_registered_namespace() => _state.Namespaces.Select(_ => _.Name.Value).ShouldEqual(["orders"]);
    [Fact] async Task should_not_append_anything() =>
        await _systemSequence.DidNotReceive().Append(Arg.Any<EventSourceId>(), Arg.Any<object>());
}
