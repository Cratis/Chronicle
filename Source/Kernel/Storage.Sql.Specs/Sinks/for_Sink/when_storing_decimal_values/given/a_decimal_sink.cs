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

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_storing_decimal_values.given;

public abstract class a_decimal_sink : Specification
{
    protected ISinkHarness _harness;
    ISink _sink;
    protected IDictionary<string, object?> _read;
    readonly Key _key = new("decimal", ArrayIndexers.NoIndexers);

    protected abstract ISinkHarness CreateHarness();

    void Establish()
    {
        _harness = CreateHarness();
        var schema = JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"string"},"amount":{"type":"number","format":"decimal"},"precise":{"type":"number","format":"decimal"}}}""");
        _sink = _harness.CreateSink(new ReadModelDefinition("decimal", "decimal_values", "Decimal", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "decimal-projection", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []));
    }

    protected async Task Store()
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("amount", null, 193.58m), new PropertyDifference("precise", null, 1234567890.123456789012345678m)])]);
        (await _sink.ApplyChanges(_key, changeset, 1UL)).ShouldBeEmpty();
        _read = (await _sink.FindOrDefault(_key))!;
    }

    void Destroy() => _harness.Dispose();
}
