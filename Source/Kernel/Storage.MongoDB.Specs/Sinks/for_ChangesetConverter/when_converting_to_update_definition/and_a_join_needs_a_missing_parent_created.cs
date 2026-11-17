// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_converting_to_update_definition;

public class and_a_join_needs_a_missing_parent_created : given.a_converter_rendering_updates
{
    Task Because() => Convert(
        new Key("key", ArrayIndexers.NoIndexers),
        42UL,
        new Joined(new ExpandoObject(), "common", new PropertyPath("joinKey"), ArrayIndexers.NoIndexers, [new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("info.name", null, "Again")])]));

    [Fact] void should_write_twice() => _joinUpdates.Count.ShouldEqual(2);
    [Fact] void should_unset_the_null_parent_first() => _joinUpdates[0].ShouldEqual(new BsonDocument("$unset", new BsonDocument("info", 1)));
    [Fact] void should_restrict_the_repair_to_documents_where_the_parent_is_null() => _joinFilters[0].ShouldEqual(new BsonDocument { { "joinKey", "common" }, { "info", new BsonDocument("$type", 10) } });
    [Fact] void should_then_set_the_property() => _joinUpdates[1]["$set"]["info.name"].ShouldEqual(new BsonString("Again"));
    [Fact] void should_then_advance_the_watermark() => _joinUpdates[1]["$max"][WellKnownProperties.LastHandledEventSequenceNumber].ShouldEqual(new BsonInt64(42));
}
