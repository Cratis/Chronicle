// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Identities;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventEntryConverter.given;

public class a_schema_aware_read : Specification
{
    protected EventEntry entry;
    protected JsonSchema? schema;
    protected IIdentityStorage identities;
    protected IExpandoObjectConverter converter;
    protected AppendedEvent result;
    protected EventType resolved_type;
    protected int schema_resolutions;
    protected IReadOnlyCollection<NamedTag> named_tags;

    void Establish()
    {
        entry = new EventEntry
        {
            Type = "sample",
            CorrelationId = Guid.Empty.ToString(),
            Content = """{"1":{"value":"first"},"2":{"value":"second"}}""",
            ContentHashes = """{"1":"first-hash","2":"second-hash"}"""
        };
        schema = JsonSchema.FromJson("""{"type":"object","properties":{"value":{"type":"string"}}}""");
        identities = Substitute.For<IIdentityStorage>();
        converter = new ExpandoObjectConverter(new TypeFormats());
        named_tags = [new NamedTag("region", "west")];
    }

    protected Task<AppendedEvent> Read() => EventEntryConverter.ToAppendedEvent(entry, "store", "namespace", identities, named_tags, ResolveSchema, converter);

    Task<JsonSchema?> ResolveSchema(EventType eventType)
    {
        resolved_type = eventType;
        schema_resolutions++;
        return Task.FromResult(schema);
    }
}
