// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_reading_an_unregistered_occurrence;

/// <summary>
/// The occurrence closes the quoted name of a SQLite PRAGMA and appends a statement dropping another table.
/// </summary>
public class and_the_name_breaks_out_of_a_pragma_on_sqlite : given.a_read_model_with_a_canary<SqlSinkHarness>
{
    const string Occurrence = "x\"); DROP TABLE canary; --";

    bool _created;

    async Task Because()
    {
        await ReadOccurrence(Occurrence);
        _created = await TableExists(Occurrence);
    }

    [Fact] async Task should_keep_the_canary() => (await CanaryExists()).ShouldBeTrue();
    [Fact] void should_not_create_a_table() => _created.ShouldBeFalse();
}
