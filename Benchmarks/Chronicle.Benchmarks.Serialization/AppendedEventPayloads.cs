// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Setup.Serialization;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventTypes;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Cratis.Chronicle.Benchmarks.Serialization;

/// <summary>
/// Creates deterministic payloads through the production schema converter.
/// </summary>
internal static class AppendedEventPayloads
{
    static readonly DateTimeOffset _occurred = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

    /// <summary>
    /// Creates services and a schema-normalized event for a payload shape.
    /// </summary>
    /// <param name="shape">The payload shape.</param>
    /// <param name="appendedEvent">The normalized event.</param>
    /// <returns>The Orleans serializer services.</returns>
    internal static ServiceProvider CreateProvider(string shape, out AppendedEvent appendedEvent)
    {
        var schema = CreateSchema(shape);
        var converter = new ExpandoObjectConverter(new TypeFormats());
        var eventType = new EventType($"codec-{shape}", EventTypeGeneration.First);
        appendedEvent = new AppendedEvent(
            EventContext.Empty with
            {
                EventType = eventType,
                EventStore = "codec-benchmark",
                EventSourceId = "customer-42",
                Occurred = _occurred,
                Causation = [
                    new(_occurred, "http", new Dictionary<string, string> { ["path"] = "/orders" }),
                    new(_occurred, "command", new Dictionary<string, string> { ["name"] = "PlaceOrder" })],
                CausedBy = new Identity("service", "Orders", OnBehalfOf: new Identity("user-42", "Customer")),
                Tags = ["orders", "benchmark"]
            },
            converter.ToExpandoObject(JsonNode.Parse(CreateDocument(shape).ToJsonString())!.AsObject(), schema));

        var eventTypes = Substitute.For<IEventTypesStorage>();
        eventTypes.GetFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>())
            .Returns(Task.FromResult(new EventTypeSchema(eventType, EventTypeOwner.Client, EventTypeSource.Code, schema)));
        var eventStore = Substitute.For<IEventStoreStorage>();
        eventStore.EventTypes.Returns(eventTypes);
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStore);

        var services = new ServiceCollection();
        services.AddOptions<ChronicleOptions>();
        services.AddSingleton<IExpandoObjectConverter>(converter);
        services.AddSingleton(storage);
        var siloBuilder = Substitute.For<ISiloBuilder>();
        siloBuilder.Services.Returns(services);
        siloBuilder.ConfigureSerialization();
        return services.BuildServiceProvider();
    }

    static JsonSchema CreateSchema(string shape)
    {
        var properties = new JsonObject
        {
            ["name"] = new JsonObject { ["type"] = "string" },
            ["count"] = Typed("integer", "int32"),
            ["id"] = Typed("string", "guid"),
            ["occurred"] = Typed("string", "date-time-offset"),
            ["active"] = new JsonObject { ["type"] = "boolean" }
        };
        if (shape != "Small")
        {
            properties["total"] = Typed("number", "decimal");
            properties["ratio"] = Typed("number", "double");
            properties["date"] = Typed("string", "date");
            properties["duration"] = Typed("string", "duration");
            properties["optional"] = new JsonObject { ["type"] = "string" };
            properties["address"] = ItemSchema();
            properties["items"] = new JsonObject { ["type"] = "array", ["items"] = ItemSchema() };
            for (var index = 0; index < 8; index++)
            {
                properties[$"field{index}"] = new JsonObject { ["type"] = "string" };
            }
        }
        return JsonSchema.FromJson(new JsonObject { ["type"] = "object", ["properties"] = properties }.ToJsonString());
    }

    static JsonObject CreateDocument(string shape)
    {
        var document = new JsonObject
        {
            ["name"] = "A realistic order",
            ["count"] = 42,
            ["id"] = "9bc1cc52-659b-40fa-845b-976ad96380f2",
            ["occurred"] = "2026-01-02T03:04:05+00:00",
            ["active"] = true
        };
        if (shape != "Small")
        {
            document["total"] = 1234.10m;
            document["ratio"] = 0.75;
            document["date"] = "2026-01-02";
            document["duration"] = "01:02:03";
            document["optional"] = null;
            document["address"] = Item(0);
            document["items"] = new JsonArray(Enumerable.Range(0, shape == "Large" ? 500 : 10).Select(index => (JsonNode)Item(index)).ToArray());
            for (var index = 0; index < 8; index++)
            {
                document[$"field{index}"] = $"Value {index}";
            }
        }
        return document;
    }

    static JsonObject Typed(string type, string format) => new() { ["type"] = type, ["format"] = format };

    static JsonObject ItemSchema() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["index"] = Typed("integer", "int32"),
            ["description"] = new JsonObject { ["type"] = "string" }
        }
    };

    static JsonObject Item(int index) => new() { ["index"] = index, ["description"] = new string('x', 100) };
}
