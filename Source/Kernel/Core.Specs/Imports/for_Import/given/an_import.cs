// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Imports;
using Cratis.Monads;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ImportsStorage = Cratis.Chronicle.Storage.InMemory.Imports.ImportStorage;
using JobError = Cratis.Choreography.Storage.Jobs.JobError;

namespace Cratis.Chronicle.Imports.for_Import.given;

public class an_import : Specification
{
    protected Import _grain;
    protected ImportKey _key;
    protected ImportGrainState _grainState;
    protected IPersistentState<ImportGrainState> _state;
    protected IGrainFactory _grainFactory;
    protected IJobsManager _jobsManager;
    protected ImportsStorage _imports;
    protected SettableTime _time;
    protected List<ImportEventsRequest> _startedJobs;
    protected ImportOperationResult _result;
    protected List<JobState> _commitJobs;
    protected Catch<IImmutableList<JobState>, JobError>? _commitJobsLookup;

    void Establish()
    {
        _key = new("store", "namespace", ImportId.New());
        _grainState = new();
        _state = Substitute.For<IPersistentState<ImportGrainState>>();
        _state.State.Returns(_grainState);
        _imports = new();
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore(_key.EventStore).GetNamespace(_key.Namespace).Imports.Returns(_imports);
        _commitJobs = [];
        storage.GetEventStore(_key.EventStore).GetNamespace(_key.Namespace).Jobs.GetJobs<IImportEvents, JobState>()
            .Returns(_ => Task.FromResult(_commitJobsLookup ?? (Catch<IImmutableList<JobState>, JobError>)_commitJobs.ToImmutableList()));
        _startedJobs = [];
        _jobsManager = Substitute.For<IJobsManager>();
        _jobsManager.Start<IImportEvents, ImportEventsRequest>(Arg.Do<ImportEventsRequest>(_startedJobs.Add))
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));
        _grainFactory = Substitute.For<IGrainFactory>();
        _grainFactory.GetGrain<IJobsManager>(0, new JobsManagerKey(_key.EventStore, _key.Namespace)).Returns(_jobsManager);
        _time = new() { Now = DateTimeOffset.UtcNow };

        _grain = new Import(_state, _grainFactory, storage, Options.Create(new ChronicleOptions()), NullLogger<Import>.Instance, _time);
        typeof(Import).GetField("_key", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(_grain, _key);
    }

    protected ImportRecord Record => _grainState.Record!;

    protected JobState CommitJob(JobStatus status, ImportId? importId = null) =>
        new() { Id = new(Guid.NewGuid()), Status = status, Request = new ImportEventsRequest(importId ?? _key.ImportId) };

    protected Task Begin(OnInvalidEvent onInvalid = OnInvalidEvent.Reject) => _grain.Begin(EventSequenceId.Log, onInvalid);

    protected async Task StageChunk(int index, int events, params ImportRejection[] rejections)
    {
        var staged = Enumerable.Range(0, events - rejections.Length)
            .Select(_ => new StagedImportEvent((index * 10) + _, "event-type+1", "Default", "source", "All", "Default", "{}", [], [], null, null, null, null, null, null, null, null))
            .ToArray();
        var chunk = new StagedImportChunk(_key.ImportId, index, index * 10, events, staged, rejections);
        await _imports.AddChunk(chunk);
        await _grain.ChunkStaged(chunk.ToSummary());
    }
}
