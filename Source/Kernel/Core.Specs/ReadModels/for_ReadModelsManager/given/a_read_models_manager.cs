// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Orleans.TestKit;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.given;

public class a_read_models_manager : Specification
{
    protected TestKitSilo _silo = new();
    protected ReadModelsManager _manager;
    protected IReadModel _readModelGrain;

    protected static readonly EventStoreName _eventStore = "some-event-store";

    async Task Establish()
    {
        _readModelGrain = Substitute.For<IReadModel>();
        _silo.AddProbe(_ => _readModelGrain);
        _manager = await _silo.CreateGrainAsync<ReadModelsManager>(_eventStore.Value);
    }

    protected static ReadModelDefinition DefinitionFor(ReadModelIdentifier identifier, ReadModelDisplayName displayName) => new(
        identifier,
        identifier.Value,
        displayName,
        ReadModelOwner.None,
        ReadModelSource.Unknown,
        ReadModelObserverType.NotSet,
        ReadModelObserverIdentifier.Unspecified,
        SinkDefinition.None,
        new Dictionary<ReadModelGeneration, Schemas.JsonSchema>(),
        []);
}
