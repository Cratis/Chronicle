// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_flattening_concurrently;

/// <summary>
/// A single <see cref="JsonSchema"/> instance is cached and shared (for example the event-type schemas held by the
/// client <c>EventTypes</c>) and flattened concurrently by constraint registration, projections, and key resolvers.
/// This exercises the lazy <c>Properties</c>/<c>Definitions</c> caches from many threads on a freshly-built schema so
/// that a reader never observes a half-populated cache (which previously surfaced as a valid property "not existing").
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
            "email": { "type": "string" },
            "reference": { "type": "string", "format": "guid" },
            "inner": { "$ref": "#/$defs/Inner" }
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
            // Fresh schema each round so every reader in the round races on the same uninitialized lazy caches.
            var schema = JsonSchema.FromJson(Json);
            using var start = new ManualResetEventSlim(false);

            var readers = Enumerable.Range(0, Readers).Select(_ => Task.Run(() =>
            {
                start.Wait();

                var flattened = schema.GetFlattenedProperties().Select(p => p.Name).ToList();
                if (!flattened.Contains("email"))
                {
                    _anomalies.Add($"flatten dropped 'email': [{string.Join(',', flattened)}]");
                }

                if (schema.GetSchemaPropertyForPropertyPath(new PropertyPath("email")) is null)
                {
                    _anomalies.Add("resolve 'email' returned null");
                }

                if (schema.GetSchemaPropertyForPropertyPath(new PropertyPath("inner.value")) is null)
                {
                    _anomalies.Add("resolve 'inner.value' returned null");
                }
            })).ToArray();

            start.Set();
            Task.WaitAll(readers);
        }
    }

    [Fact] void should_resolve_every_property_on_every_thread() => _anomalies.ShouldBeEmpty();
}
