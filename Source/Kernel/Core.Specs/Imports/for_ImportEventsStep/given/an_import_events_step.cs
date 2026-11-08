// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Imports;
using Cratis.Monads;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans.TestKit;

namespace Cratis.Chronicle.Imports.for_ImportEventsStep.given;

public class an_import_events_step : Specification
{
    protected const string StoreName = "store";
    protected const string NamespaceName = "namespace";

    protected TestKitSilo _silo = new();
    protected ImportEventsStep _step;
    protected IPersistentState<ImportEventsStepState> _stateStorage;
    protected IImport _import;
    protected IEventSequence _sequence;
    protected ImportId _importId = ImportId.New();
    protected ImportRecord? _record;
    protected Catch<JobStepResult> _result;

    async Task Establish()
    {
        _record = new ImportRecord(_importId, EventSequenceId.Log, OnInvalidEvent.Reject, DateTimeOffset.UtcNow)
        {
            State = ImportState.Validating,
            ExpectedChunks = 1,
            ExpectedEvents = 1
        };
        _import = Substitute.For<IImport>();
        _import.Get().Returns(_ => _record);
        _sequence = Substitute.For<IEventSequence>();
        _silo.AddProbe<IImport>(_ => _import);
        _silo.AddProbe<IEventSequence>(_ => _sequence);

        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        namespaceStorage.Imports.Returns(new Storage.InMemory.Imports.ImportStorage());
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        eventStoreStorage.GetNamespace(NamespaceName).Returns(namespaceStorage);
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore(StoreName).Returns(eventStoreStorage);
        _silo.AddService(storage);
        _silo.AddService(Options.Create(new ChronicleOptions()));
        _silo.AddService(Substitute.For<IJobStepThrottle>());
        var logger = _silo.AddService(NullLogger<ImportEventsStep>.Instance);
        var loggerFactory = Substitute.For<ILoggerFactory>();
        _silo.AddService(loggerFactory);
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(logger);

        _stateStorage = _silo.AddPersistentStateStorage<ImportEventsStepState>(
            nameof(ImportEventsStepState),
            Cratis.Choreography.WellKnownGrainStorageProviders.JobSteps);
        _step = await _silo.CreateGrainAsync<ImportEventsStep>(JobStepId.New(), new JobStepKey(JobId.New(), StoreName, NamespaceName));
        await _step.Prepare(new ImportEventsRequest(_importId));
    }

    protected async Task Perform(CancellationToken cancellationToken) =>
        _result = await (Task<Catch<JobStepResult>>)typeof(ImportEventsStep)
            .GetMethod("PerformStep", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_step, [_stateStorage.State, cancellationToken])!;
}
