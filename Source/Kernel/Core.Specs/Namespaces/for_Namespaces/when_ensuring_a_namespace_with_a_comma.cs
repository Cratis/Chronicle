// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Namespaces.for_Namespaces;

public class when_ensuring_a_namespace_with_a_comma : given.a_namespaces_grain
{
    Exception _error;

    void Establish() => _namespace = "raalabs,raalabs";

    async Task Because() => _error = await Catch.Exception(() => _namespaces.Ensure(_namespace));

    [Fact] void should_refuse_the_name() => _error.ShouldBeOfExactType<InvalidNamespaceName>();
    [Fact] void should_say_why() => _error.Message.ShouldContain("comma");
    [Fact] async Task should_not_append_a_namespace_added_event() => await _systemSequence.DidNotReceive().Append(Arg.Any<EventSourceId>(), Arg.Any<object>());
    [Fact] void should_not_record_the_namespace() => _state.NewNamespaces.ShouldBeEmpty();
    [Fact] async Task should_not_broadcast_the_namespace() => await _writer.DidNotReceive().Publish(Arg.Any<NamespaceAdded>());
}
