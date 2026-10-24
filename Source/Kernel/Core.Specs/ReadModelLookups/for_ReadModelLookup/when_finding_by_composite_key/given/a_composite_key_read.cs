// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModelLookups.for_ReadModelLookup.when_finding_by_composite_key.given;

public class a_composite_key_read : Specification
{
    protected static readonly EventStoreName EventStore = "store";
    protected static readonly EventStoreNamespaceName Namespace = "namespace";
    protected IGrainFactory _grains;
    protected IReadModel _readModel;
    protected IMaterializedReadModelStore _store;
    protected IExpandoObjectConverter _converter;
    protected ReadModelDefinition _definition;
    protected IDictionary<string, string> _parts;
    protected ReadModelLookup _result;
    protected ExpandoObject _instance;
    protected ExpandoObject _serialized;
    protected Key _key;
    void Establish()
    {
        _parts = new Dictionary<string, string> { ["first"] = "\"a\"", ["second"] = "42" };
        _definition = new(
            "model",
            "container",
            "Model",
            ReadModelOwner.None,
            ReadModelSource.Unknown,
            ReadModelObserverType.Projection,
            "observer",
            new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.InMemory),
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                [ReadModelGeneration.First] = JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"object","properties":{"first":{"type":"string"},"second":{"type":"integer","format":"int64"}}},"name":{"type":"string"}}}""")
            },
            []);
        _instance = new ExpandoObject();
        ((IDictionary<string, object?>)_instance)["name"] = "released";
        _grains = Substitute.For<IGrainFactory>();
        _readModel = Substitute.For<IReadModel>();
        _readModel.GetDefinition().Returns(_ => _definition);
        _grains.GetGrain<IReadModel>(Arg.Any<string>()).Returns(_readModel);
        _store = Substitute.For<IMaterializedReadModelStore>();
        _store.IsMaterialized(Arg.Any<ReadModelDefinition>()).Returns(_ => _.Arg<ReadModelDefinition>().Sink.Type != SinkTypeId.None);
        _store.FindByCompositeKey(EventStore, Namespace, Arg.Any<ReadModelDefinition>(), Arg.Do<Key>(_ => _key = _)).Returns(CompositeKeyLookupResult.Found(_instance));
        _converter = Substitute.For<IExpandoObjectConverter>();
        _converter.ToJsonObject(Arg.Do<ExpandoObject>(_ => _serialized = _), Arg.Any<JsonSchema>()).Returns(new JsonObject { ["name"] = "released" });
    }
    protected async Task Read() => _result = await ReadModelLookup.FindByCompositeKey(EventStore, Namespace, "model", _parts, _grains, _store, new TypeFormats(), _converter, new JsonSerializerOptions());
    protected Task DidNotLookUp() => _store.DidNotReceive().FindByCompositeKey(EventStore, Namespace, Arg.Any<ReadModelDefinition>(), Arg.Any<Key>());
    protected void Schema(string json) => _definition = _definition with { Schemas = new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = JsonSchema.FromJson(json) } };
}
