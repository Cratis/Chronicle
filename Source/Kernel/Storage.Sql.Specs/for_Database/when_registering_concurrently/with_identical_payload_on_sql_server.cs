// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_registering_concurrently;

[Collection(SqlServerCollection.Name)]
public class with_identical_payload_on_sql_server(SqlServerFixture fixture) : given.a_sql_server_database(fixture)
{
    (int Count, bool IsReplayable) _saved;

    async Task Because() => _saved = await given.registration_race.Run(Database);

    [Fact] void should_keep_one_registration() => _saved.Count.ShouldEqual(1);
    [Fact] void should_keep_the_registered_payload() => _saved.IsReplayable.ShouldBeTrue();
}
