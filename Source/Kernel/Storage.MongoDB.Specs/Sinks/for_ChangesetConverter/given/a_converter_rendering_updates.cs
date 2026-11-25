// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.given;

public class a_converter_rendering_updates : a_changeset_converter
{
    protected IChangeset<AppendedEvent, ExpandoObject> _changeset;
    protected UpdateDefinitionAndArrayFilters _result;
    protected BsonDocument _update;
    protected List<BsonDocument> _joinUpdates;
    protected List<BsonDocument> _joinFilters;

    void Establish()
    {
        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _joinUpdates = [];
        _joinFilters = [];

        _mongoDBConverter.ToMongoDBProperty(Arg.Any<PropertyPath>(), Arg.Any<ArrayIndexers>())
            .Returns(call => new MongoDBProperty(call.Arg<PropertyPath>().ToString(), []));
        _mongoDBConverter.ToBsonValue(Arg.Any<object?>(), Arg.Any<PropertyPath>())
            .Returns(call => call.ArgAt<object?>(0) is null ? BsonNull.Value : BsonValue.Create(call.ArgAt<object>(0)));
        _mongoDBConverter.ToBsonValue((EventSequenceNumber)42UL).Returns(new BsonInt64(42));
        _collection.UpdateManyAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _joinFilters.Add(Render(call.Arg<FilterDefinition<BsonDocument>>()));
                _joinUpdates.Add(Render(call.Arg<UpdateDefinition<BsonDocument>>()));
                return new UpdateResult.Acknowledged(1, 1, null);
            });
    }

    protected static BsonDocument Render(UpdateDefinition<BsonDocument> update) =>
        update.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry)).AsBsonDocument;

    protected static BsonDocument Render(FilterDefinition<BsonDocument> filter) =>
        filter.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry));

    protected async Task Convert(Key key, EventSequenceNumber eventSequenceNumber, params Change[] changes)
    {
        _changeset.Changes.Returns(changes);
        _result = await _converter.ToUpdateDefinition(key, _changeset, eventSequenceNumber);
        _update = _result.UpdateDefinition is null ? null : Render(_result.UpdateDefinition);
    }
}
