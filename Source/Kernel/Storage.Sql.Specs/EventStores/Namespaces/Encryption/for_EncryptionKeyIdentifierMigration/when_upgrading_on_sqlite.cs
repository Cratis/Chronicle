// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.for_EncryptionKeyIdentifierMigration;

public class when_upgrading_on_sqlite() : for_EncryptionKeyStorage.given.a_key_store(for_EncryptionKeyStorage.given.a_key_store.Sqlite())
{
    List<int> _keyRows;
    List<int> _erasureRows;

    async Task Because()
    {
        await _harness.UpgradeFromEarlierLayout(
            "INSERT INTO \"EncryptionKeys\" (\"Identifier\", \"Revision\", \"PublicKey\", \"PrivateKey\") VALUES ('Alice', 1, X'01', X'11'), ('alice', 2, X'02', X'12')",
            "INSERT INTO \"EncryptionKeyErasures\" (\"Identifier\", \"ErasedThrough\", \"ErasedKeyFingerprints\", \"NewKeyAllowed\") VALUES ('Dave', 1, '', 0)");
        _keyRows = await _harness.Query<int>($"SELECT COUNT(*) AS \"Value\" FROM \"EncryptionKeys\"");
        _erasureRows = await _harness.Query<int>($"SELECT COUNT(*) AS \"Value\" FROM \"EncryptionKeyErasures\"");
    }

    [Fact] void should_keep_every_key_row() => _keyRows.ShouldEqual([2]);
    [Fact] void should_keep_every_erasure_row() => _erasureRows.ShouldEqual([1]);
}
