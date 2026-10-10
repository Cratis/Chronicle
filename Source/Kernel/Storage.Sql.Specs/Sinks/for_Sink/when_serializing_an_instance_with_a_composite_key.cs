// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink;

/// <summary>
/// The SQL sink stores a composite key flattened to a string in the key column. The kernel serializes
/// instances for the client against the read model schema, which describes the key as an object, so the
/// flattened value must not reach the client where it would make the instance unreadable.
/// </summary>
public class when_serializing_an_instance_with_a_composite_key : Specification
{
    static readonly JsonSerializerOptions _clientOptions = new(JsonSerializerDefaults.Web);

    SqlSinkHarness _harness;
    ISink _sink;
    JsonSchema _schema;
    Exception? _error;
    ReadModel? _readModel;

    void Establish()
    {
        _harness = new SqlSinkHarness();
        _schema = JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"object","properties":{"first":{"type":"string"},"second":{"type":"integer","format":"uint64"}}},"name":{"type":"string"}}}""");
        _sink = _harness.CreateSink(new ReadModelDefinition("composite", "composite_keys", "Composite", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "composite-projection", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = _schema }, []));
    }

    async Task Because()
    {
        var keyValue = new ExpandoObject();
        ((IDictionary<string, object?>)keyValue)["first"] = "6f1c2c43-6c8c-4b3c-9d1c-3a7b4f0e2a11";
        ((IDictionary<string, object?>)keyValue)["second"] = 0UL;
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("name", null, "composite")])]);
        (await _sink.ApplyChanges(new Key(keyValue, ArrayIndexers.NoIndexers), changeset, 1UL)).ShouldBeEmpty();
        var instance = (await _sink.GetInstances()).Instances.Single();
        var json = new ExpandoObjectConverter(new TypeFormats()).ToJsonObject(instance, _schema).ToJsonString();
        _error = Catch.Exception(() => _readModel = JsonSerializer.Deserialize<ReadModel>(json, _clientOptions));
    }

    void Destroy() => _harness.Dispose();

    [Fact] void should_be_readable_by_the_client() => _error.ShouldBeNull();
    [Fact] void should_keep_the_projected_properties() => _readModel!.Name.ShouldEqual("composite");

    record CompositeKey(string First, ulong Second);

    record ReadModel(CompositeKey? Id, string Name);
}
