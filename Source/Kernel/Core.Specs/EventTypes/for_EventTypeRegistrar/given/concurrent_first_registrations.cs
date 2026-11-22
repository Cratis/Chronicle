// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventTypes;
using MemoryStorage = Cratis.Chronicle.Storage.InMemory.Events.EventTypes.EventTypesStorage;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.given;

public class concurrent_first_registrations : Specification
{
    protected const string FirstSchema = """{"type":"object","properties":{"value":{"type":"string"}}}""";
    protected string _secondSchema;
    protected MemoryStorage _memory;
    protected Exception[] _errors;
    protected IEventSequence _sequence;
    protected EventTypeDefinition _stored;
    protected JsonObjectType _successfulSchemaType;
    readonly TaskCompletionSource _bothRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _reads;
    IStorage _storage;
    IGrainFactory _grains;

    void Establish()
    {
        _memory = new();
        var types = Substitute.For<IEventTypesStorage>();
        types.GetAllDefinitions().Returns(async _ =>
        {
            var snapshot = (await _memory.GetAllDefinitions()).ToArray();
            if (Interlocked.Increment(ref _reads) == 2) _bothRead.SetResult();
            await _bothRead.Task;
            return (IEnumerable<EventTypeDefinition>)snapshot;
        });
        types.RegisterIfAbsent(Arg.Any<EventTypeToRegister>()).Returns(call => _memory.RegisterIfAbsent(call.Arg<EventTypeToRegister>()));
        types.Register(Arg.Any<IEnumerable<EventTypeToRegister>>()).Returns(call => _memory.Register(call.Arg<IEnumerable<EventTypeToRegister>>()));
        types.GetDefinition(Arg.Any<EventTypeId>()).Returns(call => _memory.GetDefinition(call.Arg<EventTypeId>()));
        var eventStore = Substitute.For<IEventStoreStorage>();
        eventStore.EventTypes.Returns(types);
        _storage = Substitute.For<IStorage>();
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStore);
        _grains = Substitute.For<IGrainFactory>();
        _sequence = Substitute.For<IEventSequence>();
        _grains.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(_sequence);
    }

    protected async Task RegisterConcurrently()
    {
        var results = await Task.WhenAll(Register(FirstSchema), Register(_secondSchema));
        _errors = results.OfType<Exception>().ToArray();
        _stored = await _memory.GetDefinition("synthetic-concurrent-event");
        _successfulSchemaType = results[0] is null ? JsonObjectType.String : (await JsonSchema.FromJsonAsync(_secondSchema)).Properties["value"].Type;
    }

    Task<Exception?> Register(string schema) => Catch.Exception(() => new EventTypeRegistrar(_grains).Register(
        "example-store",
        [new EventTypeRegistration { Type = new() { Id = "synthetic-concurrent-event", Generation = 1 }, Schema = schema }],
        false,
        _storage,
        Substitute.For<IEventTypesCacheClient>(),
        Substitute.For<IPatternCapture>()));
}
