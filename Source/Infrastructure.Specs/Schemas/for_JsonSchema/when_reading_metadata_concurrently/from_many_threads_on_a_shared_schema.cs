// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_reading_metadata_concurrently;

/// <summary>
/// The flattened property set, the compliance-metadata answer, and the resolved reference/item schemas are memoized on
/// a shared, freshly-built <see cref="JsonSchema"/>. This hammers all of them from many threads so that a reader never
/// observes a half-built memo — a dropped property, a wrong compliance answer, or an unresolved reference/item.
/// </summary>
public class from_many_threads_on_a_shared_schema : Specification
{
    const string Json = """
    {
        "type": "object",
        "$defs": {
            "Inner": { "type": "object", "title": "Inner", "properties": { "value": { "type": "string" } } }
        },
        "properties": {
            "name": { "type": "string" },
            "inner": { "$ref": "#/$defs/Inner" },
            "emails": {
                "type": "array",
                "items": { "type": "string", "compliance": [ { "metadataType": "PII", "details": "" } ] }
            }
        }
    }
    """;

    const int Rounds = 200;
    const int Readers = 16;

    ConcurrentBag<string> _anomalies;

    void Because()
    {
        _anomalies = [];

        for (var round = 0; round < Rounds; round++)
        {
            // Fresh schema each round so every reader in the round races on the same uninitialized memos.
            var schema = JsonSchema.FromJson(Json);
            using var start = new ManualResetEventSlim(false);

            var readers = Enumerable.Range(0, Readers).Select(_ => Task.Run(() =>
            {
                start.Wait();

                var flattened = schema.GetFlattenedProperties().Select(p => p.Name).ToList();
                if (!(flattened.Contains("name") && flattened.Contains("inner") && flattened.Contains("emails")))
                {
                    _anomalies.Add($"flatten dropped a property: [{string.Join(',', flattened)}]");
                }

                if (!schema.HasComplianceMetadata())
                {
                    _anomalies.Add("compliance metadata not recognized");
                }

                if (schema.ActualProperties["inner"].Reference?.Title != "Inner")
                {
                    _anomalies.Add("reference resolved to the wrong schema");
                }

                if (schema.ActualProperties["emails"].Item is null)
                {
                    _anomalies.Add("array item schema not resolved");
                }
            })).ToArray();

            start.Set();
            Task.WaitAll(readers);
        }
    }

    [Fact] void should_read_consistent_metadata_on_every_thread() => _anomalies.ShouldBeEmpty();
}
