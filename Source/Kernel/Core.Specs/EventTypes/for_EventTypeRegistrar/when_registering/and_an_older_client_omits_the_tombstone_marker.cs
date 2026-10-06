// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_registering;

public class and_an_older_client_omits_the_tombstone_marker : given.all_dependencies
{
    const string Schema = """{"type":"object","properties":{"name":{"type":"string"}}}""";
    EventTypeToRegister _registered;

    void Establish()
    {
        StoredEventTypes(StoredEventType("some-event", (1, Schema)) with { Tombstone = true });
        _eventTypesStorage.Register(Arg.Do<IEnumerable<EventTypeToRegister>>(types => _registered = types.Single())).Returns([]);
    }

    Task Because() => _subject.Register(
        "test-store",
        [new EventTypeRegistration
        {
            Type = new() { Id = "some-event", Generation = 1 },
            Schema = Schema,
            Generations = { new Contracts.Events.EventTypeGenerationDefinition { Generation = 1, Schema = Schema } }
        }],
        false,
        _storage,
        _eventTypesCacheClient,
        _patternCapture);

    [Fact] void should_keep_the_recorded_marker() => _registered.Definition.Tombstone.ShouldBeTrue();
    [Fact] void should_not_add_a_generation() => _registered.Definition.Generations.Single().Generation.Value.ShouldEqual(1u);
    [Fact] void should_not_append_a_system_event() => _grainFactory.DidNotReceive().GetGrain<EventSequences.IEventSequence>(Arg.Any<string>());
}
