// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Namespaces;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_namespace_bound;

public class binding_another_target : given.a_namespace_bound_append
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _unbound.BindNamespace(_binding with { Namespace = "other" }));

    [Fact] void should_reject_the_other_namespace() => _error.ShouldBeOfExactType<NamespaceBindingNotValid>();
    [Fact] void should_not_send_an_append() => _sequences.ReceivedCalls().ShouldBeEmpty();
}
