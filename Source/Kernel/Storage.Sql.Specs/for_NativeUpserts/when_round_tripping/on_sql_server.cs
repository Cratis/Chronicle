// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.for_NativeUpserts.when_round_tripping;

[Collection(SqlServerCollection.Name)]
public class on_sql_server(SqlServerFixture fixture) : for_Database.given.a_sql_server_database(fixture)
{
    (bool Json, bool Converted, bool Composite, bool Null, int Occurrences) _saved;

    async Task Because() => _saved = await given.round_trip.Run(Database);

    [Fact] void should_round_trip_json_collections() => _saved.Json.ShouldBeTrue();
    [Fact] void should_round_trip_concept_values() => _saved.Converted.ShouldBeTrue();
    [Fact] void should_match_the_whole_composite_key() => _saved.Composite.ShouldBeTrue();
    [Fact] void should_write_null_values() => _saved.Null.ShouldBeTrue();
    [Fact] void should_keep_distinct_replay_occurrences() => _saved.Occurrences.ShouldEqual(2);
}
