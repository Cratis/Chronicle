// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationLegacyWork.given;

public class legacy_work : Specification
{
    protected IStorage _storage;
    protected IEventStoreStorage _store;
    protected IEventStoreNamespaceStorage _namespace;
    protected GenerationLegacyWork _reader;
    protected IReadOnlyList<GenerationInitializationBlocker> _blockers;
    protected IEnumerable<ObserverState> _observers = [];
    protected IEnumerable<JobState> _replays = [];
    protected IEnumerable<JobState> _catchups = [];
    protected IReadOnlyList<ReplayContext> _contexts = [];

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _store = Substitute.For<IEventStoreStorage>();
        _namespace = Substitute.For<IEventStoreNamespaceStorage>();
        _storage.GetEventStore(new("orders")).Returns(_store);
        _store.Namespaces.GetAll().Returns([new NamespaceState(new("tenant"), default)]);
        _store.GetNamespace(new("tenant")).Returns(_namespace);
        _namespace.ReplayContexts.GetAll().Returns(_ => Task.FromResult(_contexts));
        _namespace.Jobs.GetJobs<IReplayObserver, JobState>().Returns(_ => Task.FromResult<Cratis.Monads.Catch<IImmutableList<JobState>, JobError>>(_replays.ToImmutableList()));
        _namespace.Jobs.GetJobs<ICatchUpObserver, JobState>().Returns(_ => Task.FromResult<Cratis.Monads.Catch<IImmutableList<JobState>, JobError>>(_catchups.ToImmutableList()));
        _namespace.Observers.GetAll().Returns(_ => Task.FromResult(_observers));
        _reader = new(_storage);
    }
}
