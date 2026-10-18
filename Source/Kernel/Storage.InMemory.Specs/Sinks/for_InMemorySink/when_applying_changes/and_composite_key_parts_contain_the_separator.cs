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

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_applying_changes;

public class and_composite_key_parts_contain_the_separator : Specification
{
    InMemorySinkHarness _harness;
    ISink _sink;
    Key _first;
    Key _second;
    IDictionary<string, object?>? _readFirst;
    IDictionary<string, object?>? _readSecond;

    void Establish()
    {
        _harness = new InMemorySinkHarness();
        var schema = JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"object"},"amount":{"type":"integer"}}}""");
        _sink = _harness.CreateSink(new ReadModelDefinition("payment", "payments", "Payment", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "payment-projection", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []));
        _first = new Key(CompositeKey(currency: "a", invoice: "b_c"), ArrayIndexers.NoIndexers);
        _second = new Key(CompositeKey(currency: "a_b", invoice: "c"), ArrayIndexers.NoIndexers);
    }

    async Task Because()
    {
        (await _sink.ApplyChanges(_first, ChangesetSettingAmount(1), 1UL)).ShouldBeEmpty();
        (await _sink.ApplyChanges(_second, ChangesetSettingAmount(2), 2UL)).ShouldBeEmpty();
        _readFirst = await _sink.FindOrDefault(_first);
        _readSecond = await _sink.FindOrDefault(_second);
    }

    void Destroy() => _harness.Dispose();

    [Fact] void should_keep_the_first_instance() => _readFirst!["amount"].ShouldEqual(1);
    [Fact] void should_keep_the_second_instance() => _readSecond!["amount"].ShouldEqual(2);

    static ExpandoObject CompositeKey(string currency, string invoice)
    {
        var key = new ExpandoObject();
        var values = (IDictionary<string, object?>)key;
        values["Invoice"] = invoice;
        values["Currency"] = currency;
        return key;
    }

    static IChangeset<AppendedEvent, ExpandoObject> ChangesetSettingAmount(int amount)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("amount", null, amount)])]);
        return changeset;
    }
}
