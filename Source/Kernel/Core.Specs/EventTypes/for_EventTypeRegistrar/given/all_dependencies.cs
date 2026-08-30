// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.given;

public class all_dependencies : Specification
{
    protected IStorage _storage;
    protected IEventStoreStorage _eventStoreStorage;
    protected IEventTypesStorage _eventTypesStorage;
    protected IGrainFactory _grainFactory;
    protected IEventSequence _systemEventSequence;
    protected IEventTypesCacheClient _eventTypesCacheClient;
    protected IPatternCapture _patternCapture;
    protected EventTypeRegistrar _subject;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _eventTypesStorage = Substitute.For<IEventTypesStorage>();
        _grainFactory = Substitute.For<IGrainFactory>();
        _systemEventSequence = Substitute.For<IEventSequence>();
        _eventTypesCacheClient = Substitute.For<IEventTypesCacheClient>();
        _patternCapture = Substitute.For<IPatternCapture>();
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(_eventStoreStorage);
        _eventStoreStorage.EventTypes.Returns(_eventTypesStorage);
        _grainFactory.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(_systemEventSequence);
        _eventTypesStorage.GetAllDefinitions().Returns([]);
        _eventTypesStorage.Register(Arg.Any<IEnumerable<Concepts.Events.EventTypeToRegister>>()).Returns([]);
        _subject = new EventTypeRegistrar(_grainFactory);
    }

    protected void StoredEventTypes(params Concepts.Events.EventTypeDefinition[] definitions) =>
        _eventTypesStorage.GetAllDefinitions().Returns(definitions);

    protected static Concepts.Events.EventTypeDefinition StoredEventType(string eventTypeId, params (uint Generation, string Schema)[] generations) =>
        new(
            eventTypeId,
            Concepts.Events.EventTypeOwner.Client,
            false,
            generations.Select(_ => new Concepts.Events.EventTypeGenerationDefinition(
                _.Generation,
                JsonSchema.FromJsonAsync(_.Schema).GetAwaiter().GetResult())).ToArray(),
            []);
}
