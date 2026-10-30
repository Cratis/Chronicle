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

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_reading_instances;

/// <summary>
/// The projection pipeline reads state back through <see cref="ISink.FindOrDefault"/>, which keeps returning
/// the stored flattened id: only queries rebuild the key object.
/// </summary>
public class and_the_projection_pipeline_reads_a_composite_key : Specification
{
    SqlSinkHarness _harness;
    ISink _sink;
    Key _key;
    object? _id;

    async Task Establish()
    {
        _harness = new SqlSinkHarness();
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"object","properties":{"first":{"type":"string"},"second":{"type":"integer","format":"int32"}}},"name":{"type":"string"}}}""");
        _sink = _harness.CreateSink(new ReadModelDefinition("pipeline", "pipeline_composite_keys", "Pipeline", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "pipeline-projection", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []));

        var keyValue = new ExpandoObject();
        ((IDictionary<string, object?>)keyValue)["first"] = "group";
        ((IDictionary<string, object?>)keyValue)["second"] = 7;
        _key = new Key(keyValue, ArrayIndexers.NoIndexers);
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("name", null, "pipeline")])]);
        await _sink.ApplyChanges(_key, changeset, 1UL);
    }

    async Task Because() => _id = ((IDictionary<string, object?>)(await _sink.FindOrDefault(_key))!).TryGetValue("id", out var id) ? id : null;

    void Destroy() => _harness.Dispose();

    [Fact] void should_return_the_stored_flattened_id() => _id.ShouldEqual("group_7");
}
