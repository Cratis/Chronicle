// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Services.Events.for_EventTypes.when_registering;

/// <summary>
/// Upgrading Chronicle can add a nullability marker ('?' on a format value) to an existing event type's
/// schema — e.g. a nullable known value type that previously stored 'date-time-offset' now generates
/// 'date-time-offset?'. That marker only refines how an unset value materializes; it is not a real schema
/// change, so registration must not reject it as <see cref="EventTypeSchemaChanged"/>.
/// </summary>
public class and_schema_differs_only_by_a_nullable_marker : given.all_dependencies
{
    Exception _exception;
    const string StoredSchema = """{"type":"object","properties":{"name":{"type":"string"},"occurredAt":{"type":"string","format":"date-time-offset"}}}""";
    const string MarkerSchema = """{"type":"object","properties":{"name":{"type":"string"},"occurredAt":{"type":"string","format":"date-time-offset?"}}}""";

    void Establish()
    {
        _eventTypesStorage.HasFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>()).Returns(true);
        _eventTypesStorage.GetFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>())
            .Returns(Task.FromResult(new Concepts.EventTypes.EventTypeSchema(
                new Concepts.Events.EventType("some-event", 1),
                Concepts.Events.EventTypeOwner.Client,
                Concepts.Events.EventTypeSource.Code,
                JsonSchema.FromJsonAsync(StoredSchema).GetAwaiter().GetResult())));
    }

    async Task Because() =>
        _exception = await Catch.Exception(async () => await _subject.Register(new RegisterEventTypesRequest
        {
            EventStore = "test-store",
            Types =
            [
                new EventTypeRegistration
                {
                    Type = new() { Id = "some-event", Generation = 1 },
                    Schema = MarkerSchema,
                    Generations =
                    {
                        new Contracts.Events.EventTypeGenerationDefinition { Generation = 1, Schema = MarkerSchema }
                    }
                }
            ]
        }));

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
}
