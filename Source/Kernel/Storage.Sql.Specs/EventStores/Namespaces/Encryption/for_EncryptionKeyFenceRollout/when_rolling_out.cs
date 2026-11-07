// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.for_EncryptionKeyStorage.given;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Key = Cratis.Chronicle.Storage.Compliance.EncryptionKey;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.for_EncryptionKeyFenceRollout;

public abstract class when_rolling_out(KeyStoreHarness harness) : a_key_store(harness)
{
    static readonly EventStoreName _store = EventStoreName.NotSet;
    static readonly EventStoreNamespaceName _namespace = EventStoreNamespaceName.NotSet;
    static readonly EncryptionKeyIdentifier _identifier = "Alice ";
    static readonly Key _key = new([1], [11]);

    [Fact]
    public async Task should_preserve_legacy_material_identifiers_and_erasure_metadata_on_upgrade()
    {
        await Apply(false);
        await using (var context = _harness.Context())
        {
            var legacy = EncryptionKeyIdentifierColumn.Value(context, "legacy");
            var erased = EncryptionKeyIdentifierColumn.Value(context, "erased");
            await context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"EncryptionKeys\" (\"Identifier\", \"Revision\", \"PublicKey\", \"PrivateKey\") VALUES ({legacy}, 7, {_key.Public}, {_key.Private})");
            await context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"EncryptionKeyErasures\" (\"Identifier\", \"ErasedThrough\", \"ErasedKeyFingerprints\", \"NewKeyAllowed\") VALUES ({erased}, 4, {_key.Fingerprint}, {false})");
        }

        await Apply(true);
        await using var upgraded = _harness.Context();
        var key = await upgraded.EncryptionKeys.SingleAsync();
        key.Identifier.ShouldEqual(EncryptionKeyIdentifierColumn.Value(upgraded, "legacy"));
        key.Revision.ShouldEqual(7u);
        key.PublicKey.ShouldEqual(_key.Public);
        key.PrivateKey.ShouldEqual(_key.Private);
        key.Published.ShouldBeNull();
        key.StagedLifecycle.ShouldBeNull();
        key.WriteNonce.ShouldBeNull();
        var erasure = await upgraded.EncryptionKeyErasures.SingleAsync();
        erasure.Identifier.ShouldEqual(EncryptionKeyIdentifierColumn.Value(upgraded, "erased"));
        erasure.ErasedThrough.ShouldEqual(4u);
        erasure.ErasedKeyFingerprints.ShouldEqual(_key.Fingerprint);
        erasure.NewKeyAllowed.ShouldBeFalse();
        erasure.Version.ShouldBeNull();
        erasure.StateSeal.ShouldBeNull();
        (await _harness.Storage.TryGetFor(_store, _namespace, "legacy"))!.Private.ShouldEqual(_key.Private);
        (await _harness.Storage.GetErasureFor(_store, _namespace, "erased"))!.ErasedThrough.Value.ShouldEqual(4u);
        await Assert.ThrowsAsync<EncryptionKeyErased>(() => _harness.Storage.GetOrAddFor(_store, _namespace, "erased", _key));
    }

    [Fact]
    public async Task should_fail_closed_when_an_old_shape_upsert_weakens_sealed_fields()
    {
        await _harness.Storage.SaveFor(_store, _namespace, _identifier, _key, 1u);
        await _harness.Storage.RecordErasureFor(_store, _namespace, _identifier, new("erase"));
        await using var context = _harness.Context();
        var stored = EncryptionKeyIdentifierColumn.Value(context, _identifier);
        FormattableString oldShapeUpsert;
        if (context.Database.IsSqlServer())
        {
            oldShapeUpsert = $"MERGE INTO [EncryptionKeyErasures] AS t USING (SELECT {stored} AS [Identifier]) AS s ON t.[Identifier] = s.[Identifier] WHEN MATCHED THEN UPDATE SET [ErasedThrough] = 0, [ErasedKeyFingerprints] = '', [NewKeyAllowed] = 1 WHEN NOT MATCHED THEN INSERT ([Identifier], [ErasedThrough], [ErasedKeyFingerprints], [NewKeyAllowed]) VALUES (s.[Identifier], 0, '', 1);";
        }
        else
        {
            oldShapeUpsert = $"INSERT INTO \"EncryptionKeyErasures\" (\"Identifier\", \"ErasedThrough\", \"ErasedKeyFingerprints\", \"NewKeyAllowed\") VALUES ({stored}, 0, '', {true}) ON CONFLICT (\"Identifier\") DO UPDATE SET \"ErasedThrough\" = 0, \"ErasedKeyFingerprints\" = '', \"NewKeyAllowed\" = {true}";
        }

        await context.Database.ExecuteSqlInterpolatedAsync(oldShapeUpsert);
        await Assert.ThrowsAsync<EncryptionKeyFenceUnavailable>(() => _harness.Storage.GetFenceFor(_store, _namespace, _identifier));
        await Assert.ThrowsAsync<EncryptionKeyFenceUnavailable>(() => _harness.Storage.GetOrAddFor(_store, _namespace, _identifier, _key));
        await Assert.ThrowsAsync<EncryptionKeyFenceUnavailable>(() => _harness.Storage.TryGetFor(_store, _namespace, _identifier));
    }

    [Fact]
    public async Task should_keep_explicit_revision_save_upsert_semantics()
    {
        await _harness.Storage.SaveFor(_store, _namespace, _identifier, _key, 1u);
        var replacement = new Key([2], [12]);
        await _harness.Storage.SaveFor(_store, _namespace, _identifier, replacement, 1u);
        (await _harness.Storage.TryGetFor(_store, _namespace, _identifier, 1u))!.Private.ShouldEqual(replacement.Private);
    }

    [Fact]
    public async Task should_keep_unpublished_rows_invisible_and_preserve_the_single_command_read_hit()
    {
        await _harness.Storage.SaveFor(_store, _namespace, _identifier, _key, 1u);
        await using (var context = _harness.Context())
        {
            var published = await context.EncryptionKeys.SingleAsync();
            context.EncryptionKeys.Add(new()
            {
                Identifier = published.Identifier,
                Revision = 2,
                PublicKey = [2],
                PrivateKey = [12],
                Published = false,
                StagedLifecycle = published.StagedLifecycle,
                WriteNonce = Guid.NewGuid().ToString("N")
            });
            await context.SaveChangesAsync();
        }

        _harness.CountCommands();
        (await _harness.Storage.TryGetLatestFor(_store, _namespace, _identifier))!.Revision.Value.ShouldEqual(1u);
        _harness.Commands.ShouldEqual(1);
        (await _harness.Storage.GetOrAddWithRevisionFor(_store, _namespace, _identifier, _key)).Revision.Value.ShouldEqual(1u);
        _harness.Commands.ShouldEqual(2);
        (await _harness.Storage.TryGetFor(_store, _namespace, _identifier, 2u)).ShouldBeNull();
        (await _harness.Storage.HasFor(_store, _namespace, _identifier, 2u)).ShouldBeFalse();
    }

    async Task Apply(bool up)
    {
        await using var context = _harness.Context();
        var migration = new v20_0_2 { ActiveProvider = context.Database.ProviderName };
        if (!up && context.Database.IsSqlite())
        {
            foreach (var column in migration.DownOperations.OfType<DropColumnOperation>())
            {
                var sql = $"ALTER TABLE \"{column.Table}\" DROP COLUMN \"{column.Name}\"";
                await context.Database.ExecuteSqlRawAsync(sql);
            }

            return;
        }

        foreach (var command in context.GetService<IMigrationsSqlGenerator>().Generate(up ? migration.UpOperations : migration.DownOperations))
        {
            await context.Database.ExecuteSqlRawAsync(command.CommandText);
        }
    }
}
