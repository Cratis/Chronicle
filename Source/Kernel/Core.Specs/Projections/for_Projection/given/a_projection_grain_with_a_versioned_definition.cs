// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Projections.Engine;
using Microsoft.Extensions.Options;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_Projection.given;

public class a_projection_grain_with_a_versioned_definition : Specification
{
    protected Projection _grain;
    protected TestKitSilo _silo;
    protected IStorage<ProjectionDefinition> _stateStorage;
    protected IProjectionDefinitionComparer _comparer;
    protected Storage.IStorage _storage;
    protected IObjectComparer _objectComparer;
    protected ProjectionDefinition _stored;

    async Task Establish()
    {
        _silo = new TestKitSilo();
        _stored = for_ProjectionsManager.given.a_projections_manager_grain.CreateDefinition("the-projection", "the-read-model");
        _stateStorage = Substitute.For<IStorage<ProjectionDefinition>>();
        _stateStorage.State = _stored;
        _silo.Options.StorageFactory = _ => _stateStorage;
        _comparer = Substitute.For<IProjectionDefinitionComparer>();
        _silo.AddService(_comparer);
        _silo.AddService(Substitute.For<IProjectionFactory>());
        _objectComparer = Substitute.For<IObjectComparer>();
        _silo.AddService(_objectComparer);
        _storage = Substitute.For<Storage.IStorage>();
        _storage.GetEventStore(Arg.Any<EventStoreName>()).ReadModels.GetAll().Returns([]);
        _silo.AddService(_storage);
        _silo.AddService(Options.Create(new ChronicleOptions()));
        _grain = await _silo.CreateGrainAsync<Projection>(new ProjectionKey("the-projection", "the-event-store").ToString());
        _storage.ClearReceivedCalls();
    }
}
