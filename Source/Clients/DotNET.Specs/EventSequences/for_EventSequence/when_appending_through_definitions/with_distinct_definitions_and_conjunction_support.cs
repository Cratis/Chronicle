// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_through_definitions;

public class with_distinct_definitions_and_conjunction_support : given.a_definition_backed_batch
{
    void Establish() => ((IKernelCapabilities)_connection).Capabilities.Returns([KernelCapabilities.ConcurrencyScopeConjunctions]);

    Task Because() => Append([Item("January"), OrderEvent()]);

    [Fact] void should_accept_the_batch() => _exception.ShouldBeNull();
    [Fact] void should_keep_both_predicates() => _request.ConcurrencyScopes.Count().ShouldEqual(2);
}
