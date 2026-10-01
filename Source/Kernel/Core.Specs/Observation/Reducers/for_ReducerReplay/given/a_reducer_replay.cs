// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans.TestKit;

using FailedPartition = Cratis.Chronicle.Storage.Sinks.FailedPartition;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerReplay.given;

public class a_reducer_replay : Specification
{
    protected readonly TestKitSilo _silo = new();
    protected readonly ObserverKey _key = new("reducer", "store", "namespace", EventSequenceId.Log);
    protected readonly JobId _jobId = JobId.New();
    protected ReducerReplay _replay;
    protected IReplayContexts _contexts;
    protected IJobStorage _jobs;
    protected ISink _live;
    protected ISink _shadow;
    protected IReadModelReplayManager _manager;
    protected ReplayContext? _current;
    protected ReplayContext _context;

    async Task Establish()
    {
        var storage = Substitute.For<IStorage>();
        var store = Substitute.For<IEventStoreStorage>();
        var ns = Substitute.For<IEventStoreNamespaceStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(store);
        store.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(ns);
        _contexts = Substitute.For<IReplayContexts>();
        ns.ReplayContexts.Returns(_contexts);
        _contexts.TryGet(Arg.Any<ReadModelIdentifier>()).Returns(_ => Task.FromResult(_current is null
            ? Result<ReplayContext, GetContextError>.Failed(GetContextError.NotFound)
            : Result<ReplayContext, GetContextError>.Success(_current)));
        _contexts.Save(Arg.Any<ReplayContext>()).Returns(call =>
        {
            _current = call.Arg<ReplayContext>() with { ReplayContainerName = null };
            return Task.CompletedTask;
        });
        _contexts.Evict(Arg.Any<ReadModelIdentifier>()).Returns(_ =>
        {
            _current = null;
            return Task.CompletedTask;
        });
        _jobs = Substitute.For<IJobStorage>();
        ns.Jobs.Returns(_jobs);
        _jobs.Read<JobStateWithLastHandledEvent>(Arg.Any<JobId>()).Returns(Catch<JobStateWithLastHandledEvent, Cratis.Orleans.Storage.Jobs.JobError>.Success(new JobStateWithLastHandledEvent()));
        var definition = new ReducerDefinition(new("reducer"), EventSequenceId.Log, [], "model", true, []);
        store.Reducers.Get(Arg.Any<ReducerId>()).Returns(definition);
        var readModel = new ReadModelDefinition(
            "model",
            "Model",
            "Model",
            ReadModelOwner.None,
            ReadModelSource.Code,
            ReadModelObserverType.Reducer,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = new() },
            []);
        store.ReadModels.Get(readModel.Identifier).Returns(readModel);
        _live = Substitute.For<ISink>();
        _shadow = Substitute.For<ISink>();
        _live.PublishReplay(Arg.Any<ReplayContext>(), Arg.Any<ISink>()).Returns(Task.FromResult<IEnumerable<FailedPartition>>([]));
        ns.Sinks.GetFor(Arg.Any<ReadModelDefinition>(), false).Returns(call => call.Arg<ReadModelDefinition>().ContainerName == readModel.ContainerName ? _live : _shadow);
        _manager = Substitute.For<IReadModelReplayManager>();
        _silo.AddProbe(_ => _manager);
        _silo.AddService(storage);
        _silo.AddService(NullLogger<ReducerReplay>.Instance);
        _silo.AddService(Options.Create(new Configuration.ChronicleOptions()));
        _replay = await _silo.CreateGrainAsync<ReducerReplay>(_key.ToString());
        _context = new(new("model", ReadModelGeneration.First), "Model", $"rr-attempt-{_jobId.Value:N}", DateTimeOffset.UtcNow)
        {
            ReplayContainerName = $"replay-rr-attempt-{_jobId.Value:N}",
            AllowEmptyResult = true
        };
        _current = _context;
    }
}
