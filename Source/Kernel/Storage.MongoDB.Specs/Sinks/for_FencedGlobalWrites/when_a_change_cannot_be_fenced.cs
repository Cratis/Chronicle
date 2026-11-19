// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_FencedGlobalWrites;

[Collection(MongoDBCollection.Name)]
public class when_a_change_cannot_be_fenced(MongoDBFixture fixture) : given.a_counter_shared_by_two_namespaces(fixture)
{
    Exception _exception;
    long _documentCount;

    async Task Because()
    {
        var changeset = ChangesetSettingCount(new ExpandoObject(), 1, NamespaceA, 1);
        Change[] changes = [new Removed(new ExpandoObject())];
        changeset.Changes.Returns(changes);

        _exception = await Catch.Exception(() => _sinkA.ApplyChanges(KeyOf("counter"), changeset, 1, SinkWriteMode.FencedGlobal));
        _documentCount = await _collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
    }

    [Fact] void should_refuse_it() => _exception.ShouldBeOfExactType<FencedWriteNotSupportedException>();
    [Fact] void should_write_nothing() => _documentCount.ShouldEqual(0);
}
