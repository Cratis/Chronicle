// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Storage.InMemory.Sinks;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.given;

public class a_database_with_provenance : all_dependencies
{
    protected InMemorySinkProvenanceStorage _marker;
    protected ReadModelSinkProvenance _provenance;

    void Establish()
    {
        _readModelDefinition = _readModelDefinition with
        {
            Owner = ReadModelOwner.Server,
            ObserverType = ReadModelObserverType.NotSet,
            Sink = new(SinkConfigurationId.None, WellKnownSinkTypes.InMemory)
        };
        _readModel.GetDefinition().Returns(_readModelDefinition);
        _eventStoreStorage.ReadModels.GetAll().Returns(Task.FromResult<IEnumerable<ReadModelDefinition>>([_readModelDefinition]));
        _eventStoreStorage.Projections.GetAll().Returns(Task.FromResult<IEnumerable<ProjectionDefinition>>([]));
        _eventStoreStorage.Reducers.GetAll().Returns(Task.FromResult<IEnumerable<ReducerDefinition>>([]));
        _marker = new();
        var cache = new Cratis.Chronicle.Storage.Sinks.SinkProvenanceCache();
        _sinks.GetProvenance(Arg.Any<ReadModelDefinition>()).Returns(cache.Bind("database", () => _marker));
        _provenance = new(_storage);
        _readBindings = new ReadModelReadBindings(_storage, _grainFactory, Substitute.For<IReadModelInstanceVisibilities>(), _provenance);
        _service = CreateService(_provenance);
        _sink.ClearReceivedCalls();
        _complianceHelper.ClearReceivedCalls();
    }
}
