// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Namespaces;

namespace Cratis.Chronicle.Sequences.for_AppendOnceToNamespace;

public class when_the_required_identity_is_empty : given.a_namespace_bound_keyed_endpoint
{
    void Establish() => _single = _single with { ExpectedIncarnation = NamespaceIncarnationId.NotSet };
    Task Because() => AppendSingle();

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<NamespaceIdentityRequired>();
    [Fact] void should_not_dispatch() => _eventSequence.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_look_up_a_replacement() => _incarnations.ReceivedCalls().ShouldBeEmpty();
}
