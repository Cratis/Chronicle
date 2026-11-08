// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage.MongoDB.Projections;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.ReadModels.for_ReadModel;

public class when_round_tripping_a_sink_name : Specification
{
    ReadModel _readModel;
    BsonDocument _stored;
    ReadModel _roundTripped;
    ReadModel _legacy;

    void Establish() => _readModel = new(
        "orders",
        "orders",
        "Orders",
        ReadModelOwner.Client,
        ReadModelSource.Code,
        ReadModelObserverType.Projection,
        "projection",
        new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.SQL) { Name = "reporting" },
        new Dictionary<string, BsonDocument>(),
        []);

    void Because()
    {
        _stored = _readModel.ToBsonDocument();
        _roundTripped = BsonSerializer.Deserialize<ReadModel>(_stored);
        var legacy = _stored.DeepClone().AsBsonDocument;
        legacy["sink"].AsBsonDocument.Remove("name");
        _legacy = BsonSerializer.Deserialize<ReadModel>(legacy);
    }

    [Fact] void should_store_the_name() => _stored["sink"]["name"].AsString.ShouldEqual("reporting");
    [Fact] void should_read_the_name_back() => _roundTripped.Sink.Name.ShouldEqual(new SinkName("reporting"));
    [Fact] void should_read_a_definition_without_a_name_as_the_default_sink() => _legacy.Sink.Name.ShouldEqual(SinkName.Default);
}
