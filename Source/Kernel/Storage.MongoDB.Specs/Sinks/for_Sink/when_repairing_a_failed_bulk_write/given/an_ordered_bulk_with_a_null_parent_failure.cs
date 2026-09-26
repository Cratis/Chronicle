// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Net;
using System.Reflection;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_repairing_a_failed_bulk_write.given;

public class an_ordered_bulk_with_a_null_parent_failure : Specification
{
    protected Sink _sink;
    protected IMongoCollection<BsonDocument> _collection;
    protected List<int> _batchSizes;
    protected int _repairAttempts;
    protected List<BsonDocument> _repairFilters;
    protected int _bulkAttempts;
    protected IEnumerable<FailedPartition> _failedPartitions;
    protected bool _failRepair;
    protected bool _failRetry;

    async Task Establish()
    {
        var definition = new ReadModelDefinition(
            "probe",
            "Probe",
            "Probe",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, new JsonSchema() } },
            []);
        var converter = Substitute.For<IMongoDBConverter>();
        converter.ToBsonValue(Arg.Any<Key>()).Returns(call => BsonString.Create(call.Arg<Key>().Value.ToString()));
        var collections = Substitute.For<ISinkCollections>();
        _collection = Substitute.For<IMongoCollection<BsonDocument>>();
        collections.GetCollection().Returns(_collection);
        var changesetConverter = Substitute.For<IChangesetConverter>();
        changesetConverter.ToUpdateDefinition(Arg.Any<Key>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>())
            .Returns(new UpdateDefinitionAndArrayFilters(Builders<BsonDocument>.Update.Set("outer.info.name", "ciphertext"), [], true));
        _sink = new Sink(definition, converter, collections, changesetConverter, Substitute.For<IExpandoObjectConverter>());
        _batchSizes = [];
        _repairFilters = [];
        var failure = CreateBulkFailure(1);
        _collection.BulkWriteAsync(Arg.Any<IEnumerable<WriteModel<BsonDocument>>>(), Arg.Any<BulkWriteOptions>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _bulkAttempts++;
                _batchSizes.Add(call.ArgAt<IEnumerable<WriteModel<BsonDocument>>>(0).Count());
                if (_bulkAttempts == 1)
                {
                    return Task.FromException<BulkWriteResult<BsonDocument>>(failure);
                }

                if (_failRetry && _bulkAttempts == 2)
                {
                    return Task.FromException<BulkWriteResult<BsonDocument>>(new MongoConnectionException(new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017))), "retry failed"));
                }

                return Task.FromResult<BulkWriteResult<BsonDocument>>(null!);
            });
        _collection.UpdateOneAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<UpdateDefinition<BsonDocument>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _repairAttempts++;
                _repairFilters.Add(call.ArgAt<FilterDefinition<BsonDocument>>(0).Render(new RenderArgs<BsonDocument>(BsonSerializer.LookupSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry)));
                return _failRepair ? Task.FromException<UpdateResult>(new MongoConnectionException(new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017))), "repair failed")) :
                    Task.FromResult<UpdateResult>(null!);
            });

        await _sink.BeginBulk();
        for (var index = 0; index < 3; index++)
        {
            var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
            changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference(new PropertyPath("outer.info.name"), null, "Again")])]);
            await _sink.ApplyChanges(new Key($"probe-{index}", ArrayIndexers.NoIndexers), changeset, (ulong)(42 + index));
        }
    }

    internal static MongoBulkWriteException CreateBulkFailure(int index)
    {
        var error = (BulkWriteError)typeof(BulkWriteError)
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(constructor => constructor.GetParameters().Any(parameter => parameter.ParameterType == typeof(ServerErrorCategory)))
            .Invoke([index, ServerErrorCategory.Uncategorized, 28, "Cannot create field", new BsonDocument()]);
        var connection = new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017)));
        return new MongoBulkWriteException<BsonDocument>(connection, null, [error], null, []);
    }
}
