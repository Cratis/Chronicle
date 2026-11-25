// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_FencedGlobalWrites;

[Collection(MongoDBCollection.Name)]
public class when_a_write_was_computed_from_a_stale_read(MongoDBFixture fixture) : given.a_counter_shared_by_two_namespaces(fixture)
{
    Exception _exception;
    BsonDocument _document;

    async Task Establish() => await Increment(_sinkA, NamespaceA, KeyOf("counter"), 1);

    async Task Because()
    {
        // A reads, then B writes before A does: A's changes were computed from a state that no longer exists.
        var stale = await _sinkA.FindOrDefault(KeyOf("counter"), SinkWriteMode.FencedGlobal) ?? new ExpandoObject();
        await Increment(_sinkB, NamespaceB, KeyOf("counter"), 1);

        _exception = await Catch.Exception(() => _sinkA.ApplyChanges(KeyOf("counter"), CountSetTo(stale, NamespaceA, 2), 2, SinkWriteMode.FencedGlobal));
        _document = await StoredDocument("counter");
    }

    [Fact] void should_refuse_the_write() => _exception.ShouldBeOfExactType<SinkWriteConflictException>();
    [Fact] void should_keep_what_the_other_namespace_wrote() => _document["count"].ToInt32().ShouldEqual(2);
    [Fact] void should_not_record_the_refused_event_as_handled() => _document["__lastHandled"]["namespace-a"].ToInt64().ShouldEqual(1);
}
