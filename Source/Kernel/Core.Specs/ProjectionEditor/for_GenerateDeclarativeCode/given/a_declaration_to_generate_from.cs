// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.ProjectionEditor.for_GenerateDeclarativeCode.given;

public class a_declaration_to_generate_from : Specification
{
    protected const string EventStore = "some-event-store";
    protected const string Declaration = "some declaration";
    protected const string GeneratedCode = "the generated code";
    protected static readonly ReadModelIdentifier ReadModel = new("some-read-model");

    protected IStorage _storage;
    protected IEventStoreStorage _eventStoreStorage;
    protected IReadModelDefinitionsStorage _readModelsStorage;
    protected IEventTypesStorage _eventTypesStorage;
    protected ILanguageService _languageService;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _readModelsStorage = Substitute.For<IReadModelDefinitionsStorage>();
        _eventTypesStorage = Substitute.For<IEventTypesStorage>();
        _languageService = Substitute.For<ILanguageService>();

        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(_eventStoreStorage);
        _eventStoreStorage.ReadModels.Returns(_readModelsStorage);
        _eventStoreStorage.EventTypes.Returns(_eventTypesStorage);
        _readModelsStorage.GetAll().Returns([ReadModelDefinitionFor(ReadModel)]);
        _eventTypesStorage.GetLatestForAllEventTypes().Returns([]);

        _languageService
            .GenerateDeclarativeCode(Arg.Any<ProjectionDefinition>(), Arg.Any<ReadModelDefinition>(), Arg.Any<ProjectionCodeLanguage>())
            .Returns(GeneratedCode);
    }

    protected void Compiles(ReadModelIdentifier readModel) =>
        _languageService
            .Compile(Arg.Any<string>(), Arg.Any<ProjectionOwner>(), Arg.Any<IEnumerable<ReadModelDefinition>>(), Arg.Any<IEnumerable<Concepts.EventTypes.EventTypeSchema>>())
            .Returns(DefinitionFor(readModel));

    protected void FailsToCompile(params CompilerError[] errors) =>
        _languageService
            .Compile(Arg.Any<string>(), Arg.Any<ProjectionOwner>(), Arg.Any<IEnumerable<ReadModelDefinition>>(), Arg.Any<IEnumerable<Concepts.EventTypes.EventTypeSchema>>())
            .Returns(new CompilerErrors(errors));

    protected static ReadModelDefinition ReadModelDefinitionFor(ReadModelIdentifier identifier) =>
        new(
            identifier,
            new ReadModelContainerName(identifier),
            new ReadModelDisplayName(identifier),
            ReadModelOwner.Server,
            ReadModelSource.User,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.MongoDB),
            new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, new JsonSchema() } },
            []);

    static ProjectionDefinition DefinitionFor(ReadModelIdentifier readModel) =>
        new(
            ProjectionOwner.Server,
            EventSequenceId.Log,
            new ProjectionId("some-projection"),
            readModel,
            true,
            true,
            new JsonObject(),
            new Dictionary<EventType, FromDefinition>(),
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>());
}
