// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.EventSequences.Mutations;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.Mutations.for_EventSequenceMutationRegistry;

public class when_resolving_registries_through_event_store_namespaces : given.a_mutation_registry
{
    EventSequenceMutationBeginResult _firstNamespace;
    EventSequenceMutationBeginResult _sameNamespace;
    EventSequenceMutationBeginResult _otherNamespace;

    async Task Because()
    {
        var storage = new EventStoreStorage(
            "event-store",
            _ => Substitute.For<ISinks>(),
            Substitute.For<IJobTypes>());
        var first = storage.GetNamespace("first");
        var other = storage.GetNamespace("other");

        _firstNamespace = await first.EventSequenceMutations.Begin(_request, _proposedTarget);
        _sameNamespace = await storage.GetNamespace("first").EventSequenceMutations.Begin(_request, new(20UL, 23UL, 3UL));
        _otherNamespace = await other.EventSequenceMutations.Begin(_request, _proposedTarget);
    }

    [Fact] void should_reserve_in_the_first_namespace() => _firstNamespace.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Reserved);
    [Fact] void should_share_the_registry_when_the_same_namespace_is_resolved_again() => _sameNamespace.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Resumed);
    [Fact] void should_isolate_the_same_mutation_identity_in_another_namespace() => _otherNamespace.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Reserved);
    [Fact] void should_bind_tokens_to_their_exact_namespaces() => (_firstNamespace.Token!.Scope.Namespace == (EventStoreNamespaceName)"first" && _otherNamespace.Token!.Scope.Namespace == (EventStoreNamespaceName)"other").ShouldBeTrue();
}
