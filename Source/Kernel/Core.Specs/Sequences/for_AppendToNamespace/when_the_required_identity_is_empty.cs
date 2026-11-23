// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Namespaces;

namespace Cratis.Chronicle.Sequences.for_AppendToNamespace;

public class when_the_required_identity_is_empty : given.a_protected_append_endpoint
{
    void Establish() => _command = _command with { ExpectedIncarnation = NamespaceIncarnationId.NotSet };

    Task Because() => Append();

    [Fact] void should_refuse_the_append() => _error.ShouldBeOfExactType<NamespaceIdentityRequired>();
    [Fact] void should_not_dispatch() => _eventSequence.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_lookup_a_replacement() => _incarnations.ReceivedCalls().ShouldBeEmpty();
}
