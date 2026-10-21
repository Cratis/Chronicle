// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_reads_are_protected.given;

public class protected_read_surfaces : for_ReadModels.given.all_dependencies
{
    protected Dictionary<string, Exception> _errors;

    protected async Task Exercise(ReadAllowance? allowance = null)
    {
        var unit = new UnitOfWork(CorrelationId.New(), _ => { }, _eventStore);
        using var frame = unit.EnforceReadProtection();
        using var region = allowance is null ? null : unit.AllowReads(allowance.Value, "read surface spec");
        var modelType = typeof(object);
        var queryProjections = new Projections.Projections(_eventStore, _eventTypes, Substitute.For<IClientArtifactsProvider>(), _namingPolicy, Substitute.For<IClientArtifactsActivator>(), _jsonSerializerOptions, Substitute.For<ILogger<Projections.Projections>>());
        _services.Projections.Preview(Arg.Any<Contracts.Projections.PreviewProjectionRequest>()).Returns<Task<Contracts.Primitives.OneOf<Contracts.Projections.ProjectionPreview, Contracts.Projections.ProjectionDeclarationParsingErrors>>>(_ => throw new UnknownReadModel(modelType));
        var operations = new Dictionary<string, Func<Task>>
        {
            ["projection query"] = async () => await queryProjections.Query("projection declaration"),
            ["generic instance"] = async () => await _readModels.GetInstanceById<object>("other-key"),
            ["typed instance"] = async () => await _readModels.GetInstanceById(modelType, "other-key"),
            ["instances"] = async () => await _readModels.GetInstances<object>(),
            ["snapshots"] = async () => await _readModels.GetSnapshotsById<object>("other-key"),
            ["observe"] = () => Task.FromResult(_readModels.Observe<object>()),
            ["watch"] = () => Task.FromResult(_readModels.Watch<object>()),
            ["watcher"] = () => Task.FromResult(_readModels.GetWatcherFor<object>()),
            ["materialized instances"] = async () => await _readModels.Materialized.GetInstances<object>(),
            ["materialized observe"] = () => Task.FromResult(_readModels.Materialized.ObserveInstances<object>())
        };
        _errors = [];
        foreach (var (name, operation) in operations)
        {
            _errors.Add(name, await Catch.Exception(operation));
        }
    }
}
