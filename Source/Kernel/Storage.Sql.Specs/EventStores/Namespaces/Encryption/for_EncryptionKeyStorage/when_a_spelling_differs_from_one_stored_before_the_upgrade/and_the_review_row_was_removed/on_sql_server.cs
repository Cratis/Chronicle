// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using ComplianceKey = Cratis.Chronicle.Storage.Compliance.EncryptionKey;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.for_EncryptionKeyStorage.when_a_spelling_differs_from_one_stored_before_the_upgrade.and_the_review_row_was_removed;

[Collection(SqlServerCollection.Name)]
public class on_sql_server(SqlServerFixture fixture) : given.a_key_store(new("sql_server", fixture.CreateDatabase, fixture.DropDatabase))
{
    const string LegacyAlice = "INSERT INTO [EncryptionKeys] ([Identifier], [Revision], [PublicKey], [PrivateKey]) VALUES (N'Alice', 1, 0x0A, 0x0B)";

    ComplianceKey _provisioned;

    async Task Because()
    {
        await _harness.UpgradeFromEarlierLayout(LegacyAlice);
        await _harness.Execute("DELETE FROM [EncryptionKeyIdentifierReviews]");
        _provisioned = await Provision("alice", 1);
    }

    [Fact] void should_provision_a_distinct_key() => _provisioned.Public.ShouldEqual(KeyFor(1).Public);
}
