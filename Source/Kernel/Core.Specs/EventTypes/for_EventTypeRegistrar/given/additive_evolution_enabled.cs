// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.given;

public class additive_evolution_enabled : all_dependencies
{
    protected const string Original = """{"type":"object","properties":{"name":{"type":"string"}}}""";
    protected const string Extended = """{"type":"object","properties":{"name":{"type":"string"},"age":{"type":"integer"}}}""";
    protected EventTypeDefinition _definition;

    void Establish()
    {
        _subject = new EventTypeRegistrar(
            _grainFactory,
            NullLogger<EventTypeRegistrar>.Instance,
            Options.Create(new ChronicleOptions()));
        _definition = StoredEventType("some-event", (1, Original));
        _eventTypesStorage.GetAllDefinitions().Returns(_ => new[] { _definition });
        _eventTypesStorage.TryReplaceSchema(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>(), Arg.Any<JsonSchema>(), Arg.Any<JsonSchema>())
            .Returns(call =>
            {
                var expected = call.ArgAt<JsonSchema>(2);
                if (_definition.Generations.First().Schema.ToJson() != expected.ToJson())
                {
                    return false;
                }

                _definition = _definition with { Generations = [new(1, call.ArgAt<JsonSchema>(3))] };
                return true;
            });
    }

    protected static EventTypeRegistration Registration(string schema = Extended) => new()
    {
        Type = new() { Id = "some-event", Generation = 1 },
        Owner = Contracts.Events.EventTypeOwner.Client,
        Schema = schema
    };

    protected Task Register(string schema = Extended, string eventStore = "test-store") => _subject.Register(eventStore, [Registration(schema)], false, _storage, _eventTypesCacheClient, _patternCapture);
}
