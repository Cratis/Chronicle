// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_removing_a_child.given;

public abstract class a_sink_with_composite_children<THarness> : Specification
    where THarness : ISinkHarness, new()
{
    protected IDictionary<string, object?>[] _remaining;
    protected IEnumerable<Key> _holdingTarget;
    protected IEnumerable<Key> _holdingMissing;
    THarness _harness;
    ISink _sink;

    async Task Establish()
    {
        _harness = CreateHarness();
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"children":{"type":"array","items":{"type":"object","properties":{"id":{"type":"object","properties":{"tenant":{"type":"string"},"target":{"type":"string"}}},"name":{"type":"string"}}}}}}
            """);
        var definition = new ReadModelDefinition("composite-children", "CompositeChildren", "CompositeChildren", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []);
        _sink = _harness.CreateSink(definition);
        await Apply(new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("children", null, new List<ExpandoObject> { Child("tenant", "other"), Child("tenant", "target"), Child("other", "target") })]), 1);
    }

    protected async Task RemoveCompositeChild()
    {
        _holdingTarget = await _sink.FindKeysHoldingChild(new ChildRemovedFromAll("[children]", "id", JsonSerializer.SerializeToElement(CompositeKey("tenant", "target")), ArrayIndexers.NoIndexers));
        _holdingMissing = await _sink.FindKeysHoldingChild(new ChildRemovedFromAll("[children]", "id", CompositeKey("missing", "target"), ArrayIndexers.NoIndexers));
        await Apply(new ChildRemoved(Child("tenant", "target"), "[children]", "id", CompositeKey("tenant", "target")), 2);
        var stored = (await _sink.FindOrDefault(new Key("document", ArrayIndexers.NoIndexers)))!;
        _remaining = ((IEnumerable<object>)((IDictionary<string, object?>)stored)["children"]!).Cast<IDictionary<string, object?>>().ToArray();
    }

    void Destroy() => _harness.Dispose();

    protected virtual THarness CreateHarness() => new();

    Task Apply(Change change, ulong sequence)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        changeset.Changes.Returns(new[] { change });
        return _sink.ApplyChanges(new Key("document", ArrayIndexers.NoIndexers), changeset, sequence);
    }

    static ExpandoObject CompositeKey(string tenant, string target)
    {
        dynamic key = new ExpandoObject();
        key.tenant = tenant;
        key.target = target;
        return key;
    }

    static ExpandoObject Child(string tenant, string target)
    {
        dynamic child = new ExpandoObject();
        child.id = CompositeKey(tenant, target);
        child.name = $"{tenant}/{target}";
        return child;
    }
}
