// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_looking_up_by_composite_key.given;

public abstract class a_composite_key_read_model<THarness> : for_ISink.given.an_accumulating_read_model<THarness>
    where THarness : ISinkHarness, new()
{
    protected static Key CompositeKey(string first, string second, bool reverse = false)
    {
        IDictionary<string, object?> parts = new ExpandoObject();
        if (reverse)
        {
            parts["second"] = second;
            parts["first"] = first;
        }
        else
        {
            parts["first"] = first;
            parts["second"] = second;
        }

        return new Key((ExpandoObject)parts, ArrayIndexers.NoIndexers);
    }

    protected static int Count(CompositeKeyLookupResult result) => Convert.ToInt32(((IDictionary<string, object?>)result.Instance!)["count"], CultureInfo.InvariantCulture);

    protected Task<CompositeKeyLookupResult> Lookup(Key key) => ((ISinkCompositeKeyLookup)_sink).FindByCompositeKey(key);

    protected override ReadModelDefinition CreateReadModelDefinition() => new(
        "test-read-model",
        ContainerName,
        "Test read model",
        ReadModelOwner.Client,
        ReadModelSource.Code,
        ReadModelObserverType.Projection,
        ReadModelObserverIdentifier.Unspecified,
        SinkDefinition.None,
        new Dictionary<ReadModelGeneration, JsonSchema>
        {
            [ReadModelGeneration.First] = JsonSchema.FromJson("""
                {"type":"object","properties":{"id":{"type":"object","properties":{"first":{"type":"string"},"second":{"type":"string"}}},"count":{"type":"integer"}}}
                """)
        },
        []);
}
