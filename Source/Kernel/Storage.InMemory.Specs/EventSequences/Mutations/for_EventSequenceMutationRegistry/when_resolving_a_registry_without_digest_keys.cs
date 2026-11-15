// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences.Mutations;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.Mutations.for_EventSequenceMutationRegistry;

public class when_resolving_a_registry_without_digest_keys : given.a_mutation_registry
{
    EventSequenceMutationBeginResult _begin;
    EventSequenceMutationRegistryArchiveResult _archive;

    async Task Because()
    {
        var storage = new EventStoreNamespaceStorage(
            "event-store",
            "namespace",
            Substitute.For<Cratis.Choreography.Storage.IJobsStorage>(),
            Substitute.For<ISinks>());
        _begin = await storage.EventSequenceMutations.Begin(_request, _proposedTarget);
        _archive = await storage.EventSequenceMutations.Archive(_target, null!);
    }

    [Fact] void should_fail_begin_closed() => _begin.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Corrupt);
    [Fact] void should_fail_archive_closed() => _archive.Outcome.ShouldEqual(EventSequenceMutationRegistryArchiveOutcome.Corrupt);
}
