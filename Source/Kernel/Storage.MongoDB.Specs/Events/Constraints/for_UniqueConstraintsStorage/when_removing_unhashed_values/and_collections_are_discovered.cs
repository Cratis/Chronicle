// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Chronicle.Concepts.EventSequences;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_UniqueConstraintsStorage.when_removing_unhashed_values;

public class and_collections_are_discovered : Specification
{
    UniqueConstraintsStorage _storage;
    BsonDocument _sourceFilter;
    BsonDocument _valueFilter;
    BsonRegularExpression _collectionPattern;
    (long PerEventSource, long PerValue) _counts;

    void Establish()
    {
        var database = Substitute.For<IMongoDatabase>();
        var namespaceDatabase = Substitute.For<IEventStoreNamespaceDatabase>();
        var source = Substitute.For<IMongoCollection<UniqueConstraintIndex>>();
        var values = Substitute.For<IMongoCollection<UniqueConstraintValueIndex>>();
        namespaceDatabase.GetCollection<UniqueConstraintIndex>(Arg.Any<string>()).Returns(source);
        namespaceDatabase.GetCollection<UniqueConstraintValueIndex>(Arg.Any<string>()).Returns(values);
        source.Database.Returns(database);
        var cursor = Substitute.For<IAsyncCursor<string>>();
        cursor.Current.Returns(["event-log+unique+constraint", "event-log+unique+scope+values+constraint"]);
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(true, false);
        database.ListCollectionNamesAsync(Arg.Any<ListCollectionNamesOptions>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var options = call.Arg<ListCollectionNamesOptions>();
            _collectionPattern = options.Filter.Render(new RenderArgs<BsonDocument>(BsonSerializer.LookupSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry))["name"].AsBsonRegularExpression;
            return Task.FromResult(cursor);
        });
        source.DeleteManyAsync(Arg.Any<FilterDefinition<UniqueConstraintIndex>>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _sourceFilter = call.Arg<FilterDefinition<UniqueConstraintIndex>>().Render(new RenderArgs<UniqueConstraintIndex>(BsonSerializer.LookupSerializer<UniqueConstraintIndex>(), BsonSerializer.SerializerRegistry));
            return Task.FromResult<DeleteResult>(new DeleteResult.Acknowledged(3));
        });
        values.DeleteManyAsync(Arg.Any<FilterDefinition<UniqueConstraintValueIndex>>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _valueFilter = call.Arg<FilterDefinition<UniqueConstraintValueIndex>>().Render(new RenderArgs<UniqueConstraintValueIndex>(BsonSerializer.LookupSerializer<UniqueConstraintValueIndex>(), BsonSerializer.SerializerRegistry));
            return Task.FromResult<DeleteResult>(new DeleteResult.Acknowledged(5));
        });
        _storage = new(namespaceDatabase, EventSequenceId.Log, Substitute.For<ILogger<UniqueConstraintsStorage>>());
    }

    async Task Because() => _counts = await _storage.RemoveUnhashedValues();

    [Fact] void should_try_both_claim_shapes_in_every_collection() => _counts.ShouldEqual((6L, 10L));
    [Fact] void should_only_match_source_rows_holding_a_value_field() => Condition(_sourceFilter, SourceValue)["$exists"].ShouldEqual(BsonBoolean.True);
    [Fact] void should_use_a_case_sensitive_negated_hash_pattern() => Condition(_sourceFilter, SourceValue)["$not"].AsBsonRegularExpression.ShouldEqual(new BsonRegularExpression("\\A[0-9a-f]{64}\\z"));
    [Fact] void should_only_match_retained_rows_holding_an_owner_field() => Condition(_valueFilter, RetainedOwner)["$exists"].ShouldEqual(BsonBoolean.True);
    [Fact] void should_use_the_same_hash_pattern_for_retained_values() => Condition(_valueFilter, RetainedValue)["$not"].ShouldEqual(Condition(_sourceFilter, SourceValue)["$not"]);
    [Fact] void should_not_match_another_sequence() => Regex.IsMatch("outbox+unique+constraint", _collectionPattern.Pattern, RegexOptions.None, TimeSpan.FromSeconds(1)).ShouldBeFalse();
    static string SourceValue => BsonClassMap.LookupClassMap(typeof(UniqueConstraintIndex)).GetMemberMap(nameof(UniqueConstraintIndex.Value)).ElementName;
    static string RetainedValue => BsonClassMap.LookupClassMap(typeof(UniqueConstraintValueIndex)).GetMemberMap(nameof(UniqueConstraintValueIndex.Value)).ElementName;
    static string RetainedOwner => BsonClassMap.LookupClassMap(typeof(UniqueConstraintValueIndex)).GetMemberMap(nameof(UniqueConstraintValueIndex.EventSourceId)).ElementName;

    static BsonDocument Condition(BsonDocument filter, string element)
    {
        // The driver may render a conjunction flat or as $and; merge every condition for the element either way.
        var parts = filter.Contains("$and") ? filter["$and"].AsBsonArray.Select(part => part.AsBsonDocument) : [filter];
        var condition = new BsonDocument();
        foreach (var part in parts.Where(part => part.Contains(element)))
        {
            condition.AddRange(part[element].AsBsonDocument);
        }
        return condition;
    }

    [Fact] void should_match_retained_scope_collections() => Regex.IsMatch("event-log+unique+scope+values+constraint", _collectionPattern.Pattern, RegexOptions.None, TimeSpan.FromSeconds(1)).ShouldBeTrue();
}
