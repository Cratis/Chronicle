// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator;

public class when_user_table_occupies_the_reserved_name : given.a_named_tag_migrator
{
    Exception _error;
    bool _userRowSurvived;
    bool _eventTableCreated;

    async Task Establish()
    {
        await Execute("CREATE TABLE \"__cratis_named_tags\" (UserData TEXT NOT NULL)");
        await Execute("INSERT INTO \"__cratis_named_tags\" (UserData) VALUES ('owned by user')");
    }

    async Task Because()
    {
        await using var context = CreateContext();
        _error = await Catch.Exception(() => context.EnsureTableExists());
        await using var command = _connection.CreateCommand();
        command.CommandText = "SELECT UserData FROM \"__cratis_named_tags\"";
        _userRowSurvived = (string?)await command.ExecuteScalarAsync() == "owned by user";
        _eventTableCreated = await Exists("event-sequence");
    }

    [Fact] void should_fail_by_name() => _error.ShouldBeOfExactType<NamedTagsTableCollision>();
    [Fact] void should_preserve_the_user_table() => _userRowSurvived.ShouldBeTrue();
    [Fact] void should_not_create_the_event_table() => _eventTableCreated.ShouldBeFalse();
}
