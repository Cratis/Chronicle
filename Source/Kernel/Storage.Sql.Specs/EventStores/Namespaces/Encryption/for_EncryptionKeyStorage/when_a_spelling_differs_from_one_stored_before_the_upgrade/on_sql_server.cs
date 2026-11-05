// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Sql.Sinks;
using ComplianceKey = Cratis.Chronicle.Storage.Compliance.EncryptionKey;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.for_EncryptionKeyStorage.when_a_spelling_differs_from_one_stored_before_the_upgrade;

[Collection(SqlServerCollection.Name)]
public class on_sql_server(SqlServerFixture fixture) : given.a_key_store(new("sql_server", fixture.CreateDatabase, fixture.DropDatabase))
{
    const string LegacyAlice = "INSERT INTO [EncryptionKeys] ([Identifier], [Revision], [PublicKey], [PrivateKey]) VALUES (N'Alice', 1, 0x0A, 0x0B)";

    ComplianceKey _alice;
    Exception _readFailure;
    Exception _provisionFailure;
    Exception _deleteFailure;
    Exception _erasureFailure;
    ComplianceKey _aliceAfter;
    int _rows;

    async Task Because()
    {
        await _harness.UpgradeFromEarlierLayout(LegacyAlice);
        _alice = await Read("Alice");
        _readFailure = await Catch.Exception(() => Read("alice"));
        _provisionFailure = await Catch.Exception(() => Provision("alice", 1));
        _deleteFailure = await Catch.Exception(() => _harness.Storage.DeleteFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "ALICE"));
        _erasureFailure = await Catch.Exception(() => _harness.Storage.RecordErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "alice"));
        _aliceAfter = await Read("Alice");
        _rows = (await _harness.Query<int>($"SELECT COUNT(*) AS [Value] FROM [EncryptionKeys]"))[0];
    }

    [Fact] void should_still_serve_the_stored_spelling() => _alice.Public.ShouldEqual([0x0A]);
    [Fact] void should_refuse_to_read_the_other_spelling() => _readFailure.ShouldBeOfExactType<EncryptionKeyIdentifierUnderReview>();
    [Fact] void should_refuse_to_provision_the_other_spelling() => _provisionFailure.ShouldBeOfExactType<EncryptionKeyIdentifierUnderReview>();
    [Fact] void should_refuse_to_delete_by_the_other_spelling() => _deleteFailure.ShouldBeOfExactType<EncryptionKeyIdentifierUnderReview>();
    [Fact] void should_refuse_to_erase_by_the_other_spelling() => _erasureFailure.ShouldBeOfExactType<EncryptionKeyIdentifierUnderReview>();
    [Fact] void should_keep_the_stored_key_after_the_refusals() => _aliceAfter.Public.ShouldEqual([0x0A]);
    [Fact] void should_not_add_a_row() => _rows.ShouldEqual(1);
    [Fact] void should_not_name_the_identifier_in_the_message() => _readFailure.Message.ShouldNotContain("alice");
}
