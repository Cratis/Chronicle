// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Projections;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

public class and_the_existing_read_model_key_has_a_title : given.a_projections_manager_grain
{
    ProjectionDefinition _existing;
    ProjectionDefinition _incoming;

    void Establish()
    {
        _existing = CreateDefinition("the-projection", "the-read-model");
        _incoming = CreateDefinition("the-projection", "the-read-model");
        _state.Projections = [_existing];
        _readModelDefinitions =
        [
            CreateReadModelDefinition("the-read-model") with
            {
                Schemas = new Dictionary<ReadModelGeneration, JsonSchema>
                {
                    [ReadModelGeneration.First] = JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"object","title":"OrderKey","properties":{"customerId":{"type":"string"}}}}}""")
                }
            }
        ];

        var storage = Substitute.For<IStorage>();
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        var projectionStorage = Substitute.For<IProjectionDefinitionsStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStoreStorage);
        eventStoreStorage.Projections.Returns(projectionStorage);
        projectionStorage.Has(_existing.Identifier).Returns(true);
        var comparer = new ProjectionDefinitionComparer(storage, new ObjectComparer(), NullLogger<ProjectionDefinitionComparer>.Instance);
        _definitionComparer.Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(call => comparer.Compare(call.ArgAt<ProjectionKey>(0), call.ArgAt<ProjectionDefinition>(1), call.ArgAt<ProjectionDefinition>(2)));
    }

    async Task Because() => await _grain.Register([_incoming]);

    [Fact] void should_not_reregister_the_projection() => _projectionsServiceClient.DidNotReceiveWithAnyArgs().Register(default!, default!);
    [Fact] void should_not_reset_the_projection_definition() => _projectionGrain.DidNotReceiveWithAnyArgs().SetDefinition(default!);
    [Fact] void should_not_replay_the_projection() => _observerGrain.DidNotReceiveWithAnyArgs().Replay();
}
