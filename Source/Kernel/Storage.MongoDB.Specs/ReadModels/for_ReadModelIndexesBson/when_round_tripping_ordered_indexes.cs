// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.MongoDB.Projections;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.ReadModels.for_ReadModelIndexesBson;

public class when_round_tripping_ordered_indexes : Specification
{
    IndexDefinition[] _definitions;
    IndexDefinition[] _roundTripped;
    BsonDocument _stored;

    void Establish() => _definitions = [new("name"), new("created") { Properties = [new("created", true)] }, new("name") { Properties = [new("name"), new("details.total", true)] }];

    void Because()
    {
        _stored = new BsonDocument
        {
            ["indexes"] = new BsonArray(_definitions.Select(index => index.Properties[0].Path.Path)),
            ["indexDefinitions"] = ReadModelIndexesBson.Serialize(_definitions)
        };
        var document = BsonSerializer.Deserialize<BsonDocument>(_stored.ToBson());
        _roundTripped = ReadModelIndexesBson.Deserialize(document["indexDefinitions"].AsBsonArray);
    }

    [Fact] void should_keep_the_legacy_field_as_leading_path_strings() => _stored["indexes"].AsBsonArray.Select(path => path.AsString).SequenceEqual(["name", "created", "name"]).ShouldBeTrue();
    [Fact] void should_round_trip_order_and_direction() => _roundTripped.ShouldEqual(_definitions);
    [Fact] void should_ignore_additive_fields_under_the_unchanged_storage_conventions() => BsonClassMap.LookupClassMap(typeof(ReadModel)).IgnoreExtraElements.ShouldBeTrue();
    [Fact] void should_still_read_legacy_strings() => ReadModelIndexesBson.Deserialize(new BsonArray { "name", "details.total" }).SequenceEqual([new IndexDefinition("name"), new IndexDefinition("details.total")]).ShouldBeTrue();
}
