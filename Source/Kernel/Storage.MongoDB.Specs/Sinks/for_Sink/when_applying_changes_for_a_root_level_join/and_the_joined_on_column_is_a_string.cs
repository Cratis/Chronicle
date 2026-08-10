// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_root_level_join.given;
using MongoDB.Bson;

using context = Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_root_level_join.and_the_joined_on_column_is_a_string.context;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_root_level_join;

[Collection(MongoDBCollection.Name)]
public class and_the_joined_on_column_is_a_string(context ctx) : IClassFixture<context>
{
    public class context(MongoDBFixture fixture) : a_sink_for_a_joined_read_model<StringJoinedReadModel>(fixture)
    {
        const string OrganizationNumber = "918273645";

        protected override object JoinedOnValue => OrganizationNumber;
        protected override string JoinKey => OrganizationNumber;
    }

    [Fact] void should_not_throw_from_mongodb() => ctx.Error.ShouldBeNull();
    [Fact] void should_store_the_joined_on_column_as_a_string() => ctx.StoredJoinedOnType.ShouldEqual(BsonType.String);
    [Fact] void should_stamp_the_row_the_join_matches() => ctx.StampedAfterJoin.ShouldEqual(a_sink_for_a_joined_read_model<StringJoinedReadModel>.StampedValue);
}
