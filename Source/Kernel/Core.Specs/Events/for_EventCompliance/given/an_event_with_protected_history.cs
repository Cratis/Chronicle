// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.for_EventCompliance.given;

public class an_event_with_protected_history : Specification
{
    protected IJsonSchemaMetadataManager _metadata;
    protected EventCompliance _compliance;
    protected AppendedEvent _event;
    protected AppendedEvent[] _result;
    protected Dictionary<EventType, EventTypeSchema> _schemas;
    protected string _releasedValue = "released";

    void Establish()
    {
        _metadata = Substitute.For<IJsonSchemaMetadataManager>();
        var converter = new ExpandoObjectConverter(Substitute.For<ITypeFormats>());
        _compliance = new(_metadata, converter);
        _schemas = Enumerable.Range(1, 3).ToDictionary(generation => new EventType("person", (uint)generation), generation => Schema(generation));
        var context = EventContext.Empty with { EventType = new("person", 3), Subject = "event-subject" };
        _event = new(context, converter.ToExpandoObject(JsonNode.Parse(Content(3))!.AsObject(), _schemas[context.EventType].Schema))
        {
            OriginalContent = Content(1),
            GenerationalContent = Enumerable.Range(1, 3).ToDictionary(generation => generation, Content),
            Revisions = [new(2, CorrelationId.NotSet, [], Concepts.Identities.Identity.System, DateTimeOffset.UtcNow, Content(2))]
        };
        _metadata.Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), "event-subject", Arg.Any<JsonObject>())
            .Returns(call =>
            {
                var property = call.Arg<JsonSchema>().Properties.Keys.Single();
                call.Arg<JsonObject>()[property]!.GetValue<string>().ShouldEqual("protected");
                return new JsonObject { [property] = _releasedValue };
            });
    }

    protected static string Content(int generation) => $"{{\"name{generation}\":\"protected\"}}";

    protected static string Value(string content, int generation) => JsonNode.Parse(content)![$"name{generation}"]!.GetValue<string>();

    static EventTypeSchema Schema(int generation)
    {
        var schema = JsonSchema.FromJson($"{{\"type\":\"object\",\"properties\":{{\"name{generation}\":{{\"type\":\"string\"}}}}}}");
        schema.Properties[$"name{generation}"].ExtensionData = new Dictionary<string, object?>
        {
            [ComplianceJsonSchemaExtensions.ComplianceKey] = new[] { new ComplianceSchemaMetadata("PII", string.Empty) }
        };
        return new(new("person", (uint)generation), EventTypeOwner.Client, EventTypeSource.Code, schema);
    }
}
