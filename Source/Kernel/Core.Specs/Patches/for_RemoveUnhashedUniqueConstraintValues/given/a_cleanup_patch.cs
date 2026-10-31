// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Patches;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Core.Specs.Patches.for_RemoveUnhashedUniqueConstraintValues.given;

public class a_cleanup_patch : Specification
{
    protected RemoveUnhashedUniqueConstraintValues _patch;
    protected IUniqueConstraintsStorage _log;
    protected IUniqueConstraintsStorage _other;
    protected RecordingCleanupLogger _logger;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        var grains = Substitute.For<IGrainFactory>();
        _log = Substitute.For<IUniqueConstraintsStorage>();
        _other = Substitute.For<IUniqueConstraintsStorage>();
        _logger = new();
        storage.GetEventStores().Returns(Task.FromResult<IEnumerable<EventStoreName>>(["first", "second"]));
        foreach (var store in new EventStoreName[] { "first", "second" })
        {
            var eventStore = Substitute.For<IEventStoreStorage>();
            var namespaces = Substitute.For<INamespaces>();
            storage.GetEventStore(store).Returns(eventStore);
            grains.GetGrain<INamespaces>(store).Returns(namespaces);
            namespaces.GetAll().Returns(Task.FromResult<IEnumerable<EventStoreNamespaceName>>(["one", "two"]));
            foreach (var name in new EventStoreNamespaceName[] { "one", "two" })
            {
                var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
                eventStore.GetNamespace(name).Returns(namespaceStorage);
                namespaceStorage.GetUniqueConstraintsStorage(EventSequenceId.Log).Returns(_log);
                namespaceStorage.GetUniqueConstraintsStorage(new EventSequenceId("other")).Returns(_other);
                var sequences = Substitute.For<IEventSequences>();
                grains.GetGrain<IEventSequences>(0, new EventSequencesKey(store, name)).Returns(sequences);
                sequences.GetEventSequences().Returns(Task.FromResult<IEnumerable<EventSequenceId>>([new("other"), new("other")]));
            }
        }
        _patch = new(storage, grains, _logger);
    }
}
