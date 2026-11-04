// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine.for_ProjectionsManager.given;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Projections;

namespace Cratis.Chronicle.Projections.for_ProjectionDefinitionStorageProvider.given;

public class a_history_provider : a_projections_manager
{
    protected ProjectionDefinitionStorageProvider _provider;
    protected IEventStoreStorage _eventStoreStorage;
    protected IProjectionDefinitionHistoryStorage _history;
    protected IGrainState<ProjectionDefinition> _state;
    protected GrainId _grainId;
    protected DateTimeOffset _now = DateTimeOffset.FromUnixTimeSeconds(100);

    void Establish()
    {
        _eventStoreStorage = _storage.GetEventStore(_eventStore);
        _history = _eventStoreStorage.ProjectionDefinitionHistory;
        _firstDefinition = _firstDefinition with { Version = "1.0.0", LastUpdated = _now.AddSeconds(-10) };
        _state = Substitute.For<IGrainState<ProjectionDefinition>>();
        _state.State.Returns(_firstDefinition);
        _grainId = GrainId.Create("projection", new ProjectionKey(_firstDefinition.Identifier, _eventStore).ToString());
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(_now);
        _provider = new(_storage, clock);
    }

    protected Task Write() => _provider.WriteStateAsync("projection", _grainId, _state);
}
