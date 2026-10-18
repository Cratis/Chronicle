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

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.given;

public class a_sink_with_composite_keys : Specification
{
    protected SqlSinkHarness _harness;
    protected Sink _sink;
    protected ICompositeKeyReplayRecommender _recommender;

    void Establish()
    {
        _recommender = Substitute.For<ICompositeKeyReplayRecommender>();
        _harness = new SqlSinkHarness { CompositeKeyReplayRecommender = _recommender };
        var schema = JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"object","properties":{"first":{"type":"string"},"second":{"type":"string"}}},"name":{"type":"string"}}}""");
        _sink = (Sink)_harness.CreateSink(new ReadModelDefinition("composite", "composite_keys", "Composite", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "composite-projection", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []));
    }

    void Destroy() => _harness.Dispose();

    protected static Key KeyOf(string? first, string? second)
    {
        var parts = new ExpandoObject();
        ((IDictionary<string, object?>)parts)["first"] = first;
        ((IDictionary<string, object?>)parts)["second"] = second;
        return new Key(parts, ArrayIndexers.NoIndexers);
    }

    protected async Task Write(Key key, string name)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("name", null, name)])]);
        (await _sink.ApplyChanges(key, changeset, 1UL)).ShouldBeEmpty();
    }

    protected async Task<string?> NameOf(Key key) =>
        (await _sink.FindOrDefault(key)) is IDictionary<string, object?> found ? found["name"]?.ToString() : null;
}
