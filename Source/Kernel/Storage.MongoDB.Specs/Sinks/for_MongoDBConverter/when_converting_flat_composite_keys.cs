// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_MongoDBConverter;

public class when_converting_flat_composite_keys : given.a_mongodb_converter
{
    FlatCompositeKey _key;

    void Establish()
    {
        _model = new ReadModelDefinition(
            "order",
            "orders",
            "Order",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            "projection",
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                [ReadModelGeneration.First] = JsonSchema.FromJson("""{"type":"object","properties":{"customerId":{"type":"string"},"orderNumber":{"type":"integer","format":"int32"}}}""")
            },
            []);
        _converter = new(_expandoObjectConverter, new TypeFormats(), _model, _logger);
        _key = Key(42, "customer");
    }

    [Fact]
    void should_convert_parts_using_the_root_schema_without_an_id_property()
    {
        var bson = _converter.ToBsonValue(_key);
        bson.ShouldEqual(new BsonDocument { { "customerId", "customer" }, { "orderNumber", 42 } });
        bson["orderNumber"].BsonType.ShouldEqual(BsonType.Int32);
    }

    [Fact]
    void should_filter_by_the_complete_sorted_id_document_using_the_id_index()
    {
        var filter = FlatCompositeKeyFilter.Create(_key, _converter).Render(new RenderArgs<BsonDocument>(
            BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry));
        filter.ShouldEqual(new BsonDocument("_id", new BsonDocument { { "customerId", "customer" }, { "orderNumber", 42 } }));
    }

    [Fact]
    void should_keep_distinct_tuples_and_reordered_parts_in_the_cache()
    {
        var identity = SinkCacheKey.For(_converter.ToBsonValue(_key));
        identity.ShouldEqual(SinkCacheKey.For(_converter.ToBsonValue(Key(42, "customer", reverse: true))));
        identity.ShouldNotEqual(SinkCacheKey.For(_converter.ToBsonValue(Key(43, "customer"))));
    }

    static FlatCompositeKey Key(int number, string customer, bool reverse = false)
    {
        var parts = new ExpandoObject();
        var values = (IDictionary<string, object?>)parts;
        if (reverse)
        {
            values["customerId"] = customer;
            values["orderNumber"] = number;
        }
        else
        {
            values["orderNumber"] = number;
            values["customerId"] = customer;
        }

        return new FlatCompositeKey(parts, ArrayIndexers.NoIndexers);
    }
}
