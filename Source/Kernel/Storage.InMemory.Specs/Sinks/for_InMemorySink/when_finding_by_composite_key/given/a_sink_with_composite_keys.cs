// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_finding_by_composite_key.given;

public class a_sink_with_composite_keys : Specification
{
    protected InMemorySinkHarness _harness;
    protected ISink _sink;

    void Establish()
    {
        _harness = new InMemorySinkHarness();
        var schema = JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"object"},"amount":{"type":"integer"}}}""");
        _sink = _harness.CreateSink(new ReadModelDefinition("payment", "payments", "Payment", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "payment-projection", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []));
    }

    void Destroy() => _harness.Dispose();

    protected static Key CompositeKey(params (string Name, object? Value)[] parts)
    {
        var key = new ExpandoObject();
        var values = (IDictionary<string, object?>)key;
        foreach (var (name, value) in parts)
        {
            values[name] = value;
        }

        return new Key(key, ArrayIndexers.NoIndexers);
    }

    protected static IChangeset<AppendedEvent, ExpandoObject> ChangesetSettingAmount(int amount)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("amount", null, amount)])]);
        return changeset;
    }
}
