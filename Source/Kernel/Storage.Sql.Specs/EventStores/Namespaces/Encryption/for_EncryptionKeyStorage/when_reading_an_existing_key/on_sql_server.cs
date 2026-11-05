// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using ComplianceKey = Cratis.Chronicle.Storage.Compliance.EncryptionKey;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.for_EncryptionKeyStorage.when_reading_an_existing_key;

[Collection(SqlServerCollection.Name)]
public class on_sql_server(SqlServerFixture fixture) : given.a_key_store(new("sql_server", fixture.CreateDatabase, fixture.DropDatabase))
{
    int _commands;
    ComplianceKey _key;

    async Task Because()
    {
        await Provision("Alice", 0);
        _harness.CountCommands();
        _key = await Read("Alice");
        _commands = _harness.Commands;
    }

    [Fact] void should_return_the_key() => _key.Public.ShouldEqual(KeyFor(0).Public);
    [Fact] void should_issue_one_command() => _commands.ShouldEqual(1);
}
