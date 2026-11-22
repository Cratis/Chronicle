// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.Events.for_EventTypesStorageExtensions;

public class when_getting_schemas_for_event_history : Specification
{
    IEventTypesStorage _storage;
    Dictionary<EventType, EventTypeSchema> _schemas = [];

    void Establish()
    {
        _storage = Substitute.For<IEventTypesStorage>();
        var schemas = Enumerable.Range(1, 4)
            .Select(generation => new EventTypeSchema(new("person", (uint)generation), EventTypeOwner.Client, EventTypeSource.Code, JsonSchema.FromJson("{}"))).ToArray();
        _storage.GetDefinition("person").Returns(new EventTypeDefinition("person", EventTypeOwner.Client, false, schemas.Select(schema => new EventTypeGenerationDefinition(schema.Type.Generation, schema.Schema)), []));
        _storage.GetFor(Arg.Any<IEnumerable<EventType>>()).Returns(schemas);
    }

    async Task Because()
    {
        var context = EventContext.Empty with { EventType = new("person", 3) };
        AppendedEvent @event = new(context, new ExpandoObject())
        {
            GenerationalContent = new Dictionary<int, string> { [2] = "{}", [3] = "{}" },
            OriginalContent = "{}",
            Revisions = [new(4, context.CorrelationId, [], context.CausedBy, context.Occurred, "{}")]
        };
        await _storage.EnsureSchemasFor(_schemas, [@event, @event]);
    }

    [Fact] void should_resolve_current_generational_original_and_revision_schemas() => _schemas.Keys.OrderBy(type => type.Generation.Value).ShouldEqual(Enumerable.Range(1, 4).Select(generation => new EventType("person", (uint)generation)));
    [Fact] void should_load_registered_generations_once() => _storage.Received(1).GetDefinition("person");
}
