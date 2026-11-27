// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Json;

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_reading_metadata_concurrently;

public class from_many_threads_on_shared_reference_memos : Specification
{
    const string Json = """
        {
          "properties":{
            "protected":{"$ref":"#/$defs/personal"},
            "plain":{"type":"string"},
            "unknown":{"$dynamicRef":"https://unavailable.example/schema#personal"}
          },
          "$defs":{"personal":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}
        }
        """;

    readonly ConcurrentBag<string> _anomalies = [];

    void Because() => Parallel.For(0, 32, _ =>
    {
        var schema = JsonSchema.FromJson(Json);
        Parallel.For(0, 8, _ => ReadMemos(schema));
    });

    void ReadMemos(JsonSchema schema)
    {
        var property = schema.Properties["protected"];
        if (property.MetadataReferences.Schemas.Count != 2 || property.MetadataGraph.Schemas.Count != 2)
        {
            _anomalies.Add("incomplete reference reach");
        }
        if (!ConverterSchemas.HasProtectedValueMetadata(property) || !property.HasComplianceMetadata())
        {
            _anomalies.Add("protected reference was classified as plain");
        }
        if (ConverterSchemas.HasProtectedValueMetadata(schema.Properties["plain"]))
        {
            _anomalies.Add("plain property was classified as protected");
        }
        if (!ConverterSchemas.HasProtectedValueMetadata(schema.Properties["unknown"]))
        {
            _anomalies.Add("indeterminate contextual property was classified as plain");
        }
    }

    [Fact] void should_publish_only_complete_and_consistent_memos() => _anomalies.ShouldBeEmpty();
}
