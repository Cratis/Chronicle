// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_replacing_isolated;

public abstract class and_the_key_is_composite<THarness> : given.an_isolated_read_model<THarness>
    where THarness : ISinkHarness, new()
{
    Key _other;
    int _count;
    int _otherCount;

    async Task Establish()
    {
        _key = CompositeKey(1);
        _other = CompositeKey(2);
        await _sink.ReplaceIsolated(_key, State(10), 10);
        await _sink.ReplaceIsolated(_other, State(11), 10);
    }

    async Task Because()
    {
        await _sink.ReplaceIsolated(_key, State(20), 20);
        _count = await CurrentCount();
        var other = (await _sink.FindOrDefault(_other))!;
        _otherCount = Convert.ToInt32(((IDictionary<string, object?>)other)["count"]);
    }

    [Fact] public void should_replace_the_complete_composite_instance() => _count.ShouldEqual(20);
    [Fact] public void should_not_change_the_other_instance() => _otherCount.ShouldEqual(11);

    protected override ReadModelDefinition CreateReadModelDefinition() => base.CreateReadModelDefinition() with
    {
        Schemas = new Dictionary<ReadModelGeneration, JsonSchema>
        {
            [ReadModelGeneration.First] = JsonSchema.FromJson("""
                {"type":"object","properties":{"id":{"type":"object","properties":{"a":{"type":"integer"},"b":{"type":"string"}}},"count":{"type":"integer"}}}
                """)
        }
    };

    static Key CompositeKey(int a)
    {
        var value = new ExpandoObject();
        ((IDictionary<string, object?>)value)["a"] = a;
        ((IDictionary<string, object?>)value)["b"] = "group";
        return new Key(value, ArrayIndexers.NoIndexers);
    }
}
