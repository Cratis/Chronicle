// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSourceIds.for_spelling_variants.when_reading_events;

[Collection(SqlServerCollection.Name)]
public class and_ids_differ_by_width_or_accent_on_sql_server(SqlServerFixture fixture) : given.a_spelling_storage(new("sql_server", fixture.CreateDatabase, fixture.DropDatabase))
{
    const string Plain = "Alice";
    const string FullWidth = "\uFF21lice";
    const string Accented = "Alic\u00E9";
    ulong[] _plain;
    ulong[] _fullWidth;
    ulong[] _accented;

    async Task Because()
    {
        await _harness.Append(Plain);
        await _harness.Append(FullWidth);
        await _harness.Append(Accented);
        _plain = await _harness.Read(Plain);
        _fullWidth = await _harness.Read(FullWidth);
        _accented = await _harness.Read(Accented);
    }

    [Fact] void should_return_only_the_events_of_the_plain_id() => _plain.ShouldEqual([0UL]);
    [Fact] void should_return_only_the_events_of_the_full_width_id() => _fullWidth.ShouldEqual([1UL]);
    [Fact] void should_return_only_the_events_of_the_accented_id() => _accented.ShouldEqual([2UL]);
}
