// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Namespaces;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_activating;

public class and_the_namespace_is_a_case_variant : given.an_event_sequence
{
    Exception _exception;

    protected override async Task<EventSequence> CreateEventSequence()
    {
        _namespaces.EnsureCanonical(EventStoreNamespace).Returns((EventStoreNamespaceName)"Test-Namespace");
        _exception = await Catch.Exception(base.CreateEventSequence);
        return null!;
    }

    [Fact] void should_refuse_activation() => _exception.ShouldBeOfExactType<NamespaceIsCaseVariant>();
    [Fact] async Task should_not_initialize_constraints() => await _constraintsGrain.DidNotReceive().GetVersion();
}
