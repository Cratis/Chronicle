// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkParity.when_reducing_string_arrays;

[Collection(MongoDBCollection.Name)]
public class and_values_are_plain(MongoDBFixture fixture) : given.a_string_array_round_trip(fixture)
{
    protected override string ArrayMetadata => string.Empty;

    Task Because() => ApplyStates();

    [Fact] void should_read_every_prior_state_through_typed_json() => Reductions.ShouldEqual(16);
    [Fact] void should_keep_both_sinks_equivalent() => ParityReport.ShouldEqual(string.Empty);
    [Fact] void should_store_all_involved_users_as_an_array() => StoredDocument!["involvedUsers"].ShouldEqual(new BsonArray { "final", "two" });
    [Fact] void should_store_all_assignee_logins_as_an_array() => StoredDocument!["assigneeLogins"].ShouldEqual(new BsonArray { "final", "two" });
}
