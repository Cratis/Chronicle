// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelsForTesting.when_reads_are_protected.given;

public class protected_surfaces : Specification
{
    protected EventStoreForTesting _store;
    protected EventSourceId _source;
    protected Dictionary<string, Exception> _errors;
    PreparedEvent _prepared;

    async Task Establish()
    {
        _store = new EventStoreForTesting();
        _source = EventSourceId.New();
        var created = new ModuleCreated("Test");
        _prepared = await _store.EventLog.Prepare(created);
        await _store.EventLog.AppendPrepared(_source, _prepared);
    }

    protected async Task ExerciseLog(ReadAllowance? allowance)
    {
        var unit = _store.UnitOfWorkManager.Begin(CorrelationId.New());
        using var frame = unit.EnforceReadProtection();
        using var region = allowance is null ? null : unit.AllowReads(allowance.Value, "log read matrix");
        var log = _store.EventLog;
        await Capture(new Dictionary<string, Func<Task>>
        {
            ["verify"] = () => log.VerifyContent(EventSequenceNumber.First, _prepared),
            ["has events"] = () => log.HasEventsFor(_source),
            ["metadata"] = () => log.GetMetadataAt(1000),
            ["metadata batch"] = () => log.GetMetadataAt([EventSequenceNumber.First]),
            ["from position"] = () => log.GetFromSequenceNumber(EventSequenceNumber.First),
            ["filtered"] = () => log.GetForEventSourceIdAndEventTypes(_source, [_store.EventTypes.GetEventTypeFor(typeof(ModuleCreated))]),
            ["next"] = log.GetNextSequenceNumber,
            ["tail"] = () => log.GetTailSequenceNumber(),
            ["observer tail"] = () => log.GetTailSequenceNumberForObserver(typeof(object)),
            ["closed streams"] = () => log.GetClosedStreams(),
            ["completed stream"] = () => log.IsStreamCompleted("stream", "id"),
            ["completed scope"] = () => log.IsStreamCompleted(ClosedStreamScope.ForEventSource(_source))
        });
    }

    protected async Task ExerciseModels()
    {
        var unit = _store.UnitOfWorkManager.Begin(CorrelationId.New());
        using var frame = unit.EnforceReadProtection();
        var modelType = typeof(SimpleModule);
        await Capture(new Dictionary<string, Func<Task>>
        {
            ["generic instance"] = () => _store.ReadModels.GetInstanceById<SimpleModule>(_source),
            ["typed instance"] = () => _store.ReadModels.GetInstanceById(modelType, _source),
            ["instances"] = () => _store.ReadModels.GetInstances<SimpleModule>(),
            ["snapshots"] = () => _store.ReadModels.GetSnapshotsById<SimpleModule>(_source),
            ["observe"] = () => Task.FromResult(_store.ReadModels.Observe<SimpleModule>()),
            ["watch"] = () => Task.FromResult(_store.ReadModels.Watch<SimpleModule>()),
            ["watcher"] = () => Task.FromResult(_store.ReadModels.GetWatcherFor<SimpleModule>()),
            ["materialized instances"] = () => _store.ReadModels.Materialized.GetInstances<SimpleModule>(),
            ["materialized observe"] = () => Task.FromResult(_store.ReadModels.Materialized.ObserveInstances<SimpleModule>()),
            ["reducer instance"] = () => _store.Reducers.GetInstanceById(typeof(SimpleModule), _source),
            ["reducer instances"] = () => _store.Reducers.GetInstances(typeof(SimpleModule)),
            ["namespaces"] = () => _store.GetNamespaces(),
            ["another sequence"] = () => _store.GetEventSequence("another-sequence").HasEventsFor("other-key")
        });
    }

    void Destroy() => _store.Dispose();

    async Task Capture(Dictionary<string, Func<Task>> operations)
    {
        _errors = [];
        foreach (var (name, operation) in operations)
        {
            _errors.Add(name, await Catch.Exception(operation));
        }
    }
}
