// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Json;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Projections.for_ProjectionFuturesStorage.given;

public class a_futures_storage : Namespaces.given.a_migrated_namespace_database
{
    protected ProjectionFuturesStorage _storage;
    protected ProjectionFuture _future;
    protected readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new EnumConverterFactory(),
            new EnumerableConceptAsJsonConverterFactory(),
            new ConceptAsJsonConverterFactory()
        }
    };

    void Establish()
    {
        _storage = new(_eventStore, _namespace, _database, _options);
        var context = new EventContext(
            new EventType("ChildCreated", 3, true),
            "order",
            "child-42",
            "fulfillment",
            "shipment-7",
            42,
            DateTimeOffset.Parse("2026-05-12T13:14:15.1234567+02:00"),
            "orders",
            "tenant-7",
            Guid.Parse("de001d50-d624-4d65-a550-314241d40e07"),
            [new Causation(DateTimeOffset.Parse("2026-05-12T12:00:00+02:00"), "command", new Dictionary<string, string> { ["name"] = "CreateChild" })],
            new Identity("user-7", "Alice", "alice", new Identity("service-9", "Importer", "importer")),
            [new Tag("priority"), new Tag("imported")],
            "content-hash",
            EventObservationState.Replay,
            new Subject("person-17"))
        {
            EventSource = "Orders",
            NamedTags = [new NamedTag(new TagName("region"), "north")]
        };
        _future = new(
            ProjectionFutureId.New(),
            new ProjectionId("projection"),
            new AppendedEvent(context, new ExpandoObject()),
            PropertyPath.Root,
            new PropertyPath("children"),
            new PropertyPath("id"),
            new PropertyPath("parentId"),
            new Key("parent", ArrayIndexers.NoIndexers),
            DateTimeOffset.Parse("2026-05-12T14:00:00Z"));
    }
}
