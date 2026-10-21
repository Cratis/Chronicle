// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_registering_concurrently;

public class with_identical_payload_on_sqlite : given.a_sqlite_database
{
    (int Count, bool IsReplayable) _saved;

    async Task Because() => _saved = await given.registration_race.Run(_database);

    [Fact] void should_keep_one_registration() => _saved.Count.ShouldEqual(1);
    [Fact] void should_keep_the_registered_payload() => _saved.IsReplayable.ShouldBeTrue();
}
