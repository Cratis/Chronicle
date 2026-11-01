// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace Cratis.Chronicle.Setup.Serialization.for_AppendedEventSerializer.given;

public class a_normalized_event : Specification
{
    const string SchemaDefinition =
            """
            {
                "type": "object",
                "properties": {
                    "text": { "type": "string" },
                    "signedByte": { "type": "integer", "format": "int8" },
                    "unsignedByte": { "type": "integer", "format": "uint8" },
                    "signedShort": { "type": "integer", "format": "int16" },
                    "unsignedShort": { "type": "integer", "format": "uint16" },
                    "signedInt": { "type": "integer", "format": "int32" },
                    "unsignedInt": { "type": "integer", "format": "uint32" },
                    "signedLong": { "type": "integer", "format": "int64" },
                    "unsignedLong": { "type": "integer", "format": "uint64" },
                    "single": { "type": "number", "format": "float" },
                    "double": { "type": "number", "format": "double" },
                    "decimal": { "type": "number", "format": "decimal" },
                    "id": { "type": "string", "format": "guid" },
                    "timestamp": { "type": "string", "format": "date-time-offset" },
                    "datetime": { "type": "string", "format": "date-time" },
                    "date": { "type": "string", "format": "date" },
                    "duration": { "type": "string", "format": "duration" },
                    "time": { "type": "string", "format": "time" },
                    "bytes": { "type": "string", "format": "byte-array" },
                    "point": { "type": "object", "format": "point" },
                    "line": { "type": "object", "format": "linestring" },
                    "polygon": { "type": "object", "format": "polygon" },
                    "payload": { "type": "object", "additionalProperties": {} },
                    "optional": { "type": ["string", "null"] },
                    "empty": { "type": "array", "items": { "type": "string" } },
                    "nested": { "type": "object", "properties": { "value": { "type": "string" } } }
                }
            }
            """;

    const string Content =
            """
            {
                "text": "An order — \u0000",
                "signedByte": -128,
                "unsignedByte": 255,
                "signedShort": -32768,
                "unsignedShort": 65535,
                "signedInt": -2147483648,
                "unsignedInt": 4294967295,
                "signedLong": -9223372036854775808,
                "unsignedLong": 18446744073709551615,
                "single": 1.5,
                "double": 1.25,
                "decimal": 1234567890.123456789012345678,
                "id": "9bc1cc52-659b-40fa-845b-976ad96380f2",
                "timestamp": "2026-01-02T03:04:05.1234567+05:30",
                "datetime": "2026-01-02T03:04:05.1234567Z",
                "date": "2026-01-02",
                "duration": "1.02:03:04.1234567",
                "time": "03:04:05.1234567",
                "bytes": "AAEC/w==",
                "point": { "type": "Point", "coordinates": [1.5, 2.5] },
                "line": { "type": "LineString", "coordinates": [[1, 2], [3, 4]] },
                "polygon": { "type": "Polygon", "coordinates": [[[0, 0], [1, 0], [1, 1], [0, 0]]] },
                "payload": { "Name": "upper", "name": "lower", "nil": null, "items": [null, { "Name": "a", "name": "b" }] },
                "optional": null,
                "empty": [],
                "nested": { "value": "nested" }
            }
            """;

    protected ServiceProvider service_provider;
    protected Serializer<AppendedEvent> serializer;
    protected JsonSerializerOptions options;
    protected ExpandoObjectConverter converter;
    protected JsonSchema schema;
    protected AppendedEvent appended_event;
    protected AppendedEvent[] event_batch;
    protected byte[] released_format;

    void Establish()
    {
        schema = JsonSchema.FromJson(SchemaDefinition);
        var document = JsonNode.Parse(Content)!.AsObject();
        converter = new(new TypeFormats());
        var context = EventContext.Empty with
        {
            EventType = new("normalized", EventTypeGeneration.First),
            EventSourceId = "source-42",
            Occurred = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
            CorrelationId = new(Guid.Parse("c84d9bf4-a04e-4dcc-b031-af18a078d89d")),
            Causation = [new(new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "command", new Dictionary<string, string> { ["name"] = "PlaceOrder" })],
            CausedBy = new("service", "Orders", OnBehalfOf: new("user-42", "Customer")),
            Tags = ["orders", "codec"]
        };
        appended_event = new(context, converter.ToExpandoObject(document, schema));
        event_batch = [
            appended_event,
            new(context with { SequenceNumber = 1 }, converter.ToExpandoObject(JsonNode.Parse("""{"text":"sparse","nested":{"value":"second"},"payload":{"Name":"upper","name":"lower"}}""")!.AsObject(), schema)),
            new(context with { SequenceNumber = 2 }, converter.ToExpandoObject(new JsonObject(), schema))];

        var services = new ServiceCollection();
        var silo = Substitute.For<ISiloBuilder>();
        silo.Services.Returns(services);
        silo.ConfigureSerialization();
        var cache = Substitute.For<IEventTypeSchemaCache>();
        cache.GetSchemaJsonFor(context.EventStore, context.EventType.Id, context.EventType.Generation).Returns(schema.ToJson());
        services.AddOptions<ChronicleOptions>();
        services.AddSingleton(cache);
        services.AddSingleton<IExpandoObjectConverter>(converter);
        service_provider = services.BuildServiceProvider();
        options = service_provider.GetRequiredService<JsonSerializerOptions>();
        serializer = service_provider.GetRequiredService<Serializer<AppendedEvent>>();

        // Reference the unchanged v19.115.0 writer (16564234a898b79ffc6eb69fcbec9f148b0d56ca):
        // serialize { appendedEvent, schema }, then StringCodec.
        var releasedEnvelope = new
        {
            AppendedEvent = appended_event,
            Schema = schema.ToJson()
        };
        var json = JsonSerializer.Serialize(releasedEnvelope, options);
        released_format = service_provider.GetRequiredService<Serializer<string>>().SerializeToArray(json);
    }

    protected static IEnumerable<Type?> ContentTypes(object? content) => content switch
    {
        ExpandoObject nested => nested.SelectMany(property => ContentTypes(property.Value)).Prepend(content.GetType()),
        object[] array => array.SelectMany(ContentTypes).Prepend(content.GetType()),
        null => [null],
        _ => [content.GetType()]
    };

    void Destroy() => service_provider.Dispose();
}
