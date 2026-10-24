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
using Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ensuring_indexes;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_reading_instances.given;

public class a_sink_with_unreadable_composite_keys : Specification
{
    protected const string SeparatorReason = "a key part contains '_' or is missing";
    protected const string EmptyPartReason = "a key part is empty";

    protected CapturingIndexLogger _logger;
    protected SqlSinkHarness _harness;
    protected ISink _sink;

    async Task Establish()
    {
        _logger = new CapturingIndexLogger();
        _harness = new SqlSinkHarness { Logger = _logger };
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"object","properties":{"first":{"type":"string"},"second":{"type":"integer","format":"int32"}}},"name":{"type":"string"}}}""");
        _sink = _harness.CreateSink(new ReadModelDefinition("unreadable", "unreadable_composite_keys", "Unreadable", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "unreadable-projection", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []));
        await AddInstance("a_b", 1);
        await AddInstance("c_d", 2);
        await AddInstance(string.Empty, 3);
        await AddInstance(string.Empty, 4);
    }

    void Destroy() => _harness.Dispose();

    async Task AddInstance(string first, int second)
    {
        var keyValue = new ExpandoObject();
        ((IDictionary<string, object?>)keyValue)["first"] = first;
        ((IDictionary<string, object?>)keyValue)["second"] = second;
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("name", null, "unreadable")])]);
        await _sink.ApplyChanges(new Key(keyValue, ArrayIndexers.NoIndexers), changeset, 1UL);
    }
}
