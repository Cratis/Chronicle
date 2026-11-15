// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.for_EncryptionKeyIdentifierMigration;

[Collection(PostgreSqlCollection.Name)]
public class when_upgrading_on_postgresql(PostgreSqlFixture fixture) : for_EncryptionKeyStorage.given.a_key_store(new("postgresql", fixture.CreateDatabase, fixture.DropDatabase))
{
    List<int> _keyLengths;
    List<int> _erasureLengths;
    int _keyRows;
    int _erasureRows;
    List<int> _reviewTables;

    async Task Because()
    {
        await _harness.UpgradeFromEarlierLayout(
            "INSERT INTO \"EncryptionKeys\" (\"Identifier\", \"Revision\", \"PublicKey\", \"PrivateKey\") VALUES ('Alice', 1, '\\x01', '\\x11'), ('alice', 2, '\\x02', '\\x12'), ('bob ', 1, '\\x03', '\\x13')",
            "INSERT INTO \"EncryptionKeyErasures\" (\"Identifier\", \"ErasedThrough\", \"ErasedKeyFingerprints\", \"NewKeyAllowed\") VALUES ('Dave', 1, '', false)");
        _keyLengths = await _harness.Query<int>($"SELECT character_maximum_length AS \"Value\" FROM information_schema.columns WHERE table_name = 'EncryptionKeys' AND column_name = 'Identifier'");
        _erasureLengths = await _harness.Query<int>($"SELECT character_maximum_length AS \"Value\" FROM information_schema.columns WHERE table_name = 'EncryptionKeyErasures' AND column_name = 'Identifier'");
        _keyRows = (await _harness.Query<int>($"SELECT COUNT(*)::int AS \"Value\" FROM \"EncryptionKeys\""))[0];
        _erasureRows = (await _harness.Query<int>($"SELECT COUNT(*)::int AS \"Value\" FROM \"EncryptionKeyErasures\""))[0];
        _reviewTables = await _harness.Query<int>($"SELECT COUNT(*)::int AS \"Value\" FROM information_schema.tables WHERE table_name = 'EncryptionKeyIdentifierReviews'");
    }

    [Fact] void should_widen_the_key_identifier_to_256_characters() => _keyLengths.ShouldEqual([256]);
    [Fact] void should_widen_the_erasure_identifier_to_256_characters() => _erasureLengths.ShouldEqual([256]);
    [Fact] void should_keep_every_key_row() => _keyRows.ShouldEqual(3);
    [Fact] void should_keep_every_erasure_row() => _erasureRows.ShouldEqual(1);
    [Fact] void should_not_create_a_review_table() => _reviewTables.ShouldEqual([0]);
}
