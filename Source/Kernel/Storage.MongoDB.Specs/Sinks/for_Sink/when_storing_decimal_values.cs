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

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink;

[Collection(MongoDBCollection.Name)]
public class when_storing_decimal_values(MongoDBFixture fixture) : Specification
{
    MongoSinkHarness _harness;
    ISink _sink;
    IDictionary<string, object?> _read;
    readonly Key _key = new("decimal", ArrayIndexers.NoIndexers);

    void Establish()
    {
        _harness = new MongoSinkHarness { Fixture = fixture };
        var schema = JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"string"},"amount":{"type":"number","format":"decimal"},"precise":{"type":"number","format":"decimal"}}}""");
        _sink = _harness.CreateSink(new ReadModelDefinition("decimal", "decimal_values", "Decimal", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "decimal-projection", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []));
    }

    async Task Because()
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [
            new PropertyDifference("amount", null, 193.58m), new PropertyDifference("precise", null, 1234567890.123456789012345678m)])]);
        (await _sink.ApplyChanges(_key, changeset, 1UL)).ShouldBeEmpty();
        _read = (await _sink.FindOrDefault(_key))!;
    }

    void Destroy() => _harness.Dispose();

    [Fact] void should_preserve_amount_bits() => decimal.GetBits((decimal)_read["amount"]!).ShouldEqual(decimal.GetBits(193.58m));
    [Fact] void should_preserve_all_significant_digits() => decimal.GetBits((decimal)_read["precise"]!).ShouldEqual(decimal.GetBits(1234567890.123456789012345678m));
}
