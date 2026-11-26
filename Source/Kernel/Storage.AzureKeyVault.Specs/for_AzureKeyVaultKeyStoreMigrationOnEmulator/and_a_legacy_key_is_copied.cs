// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_name;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultKeyStoreMigrationOnEmulator;

[Collection(AzureKeyVaultCollection.Name)]
public class and_a_legacy_key_is_copied(and_a_legacy_key_is_copied.context ctx) : IClassFixture<and_a_legacy_key_is_copied.context>
{
    public class context(AzureKeyVaultFixture fixture) : given.a_migration_on_the_emulator(fixture)
    {
        public EncryptionKeyMigrationReport Report = default!;
        public bool CanonicalIsLive;
        public bool LegacyIsLive;
        public EncryptionKey? FromRuntime;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            await WriteLegacyKey(1, FirstKey);
            Report = await Migrate();
            CanonicalIsLive = await LegacySecrets.IsLive(Fixture.SecretClient, Current(1));
            LegacyIsLive = await LegacySecrets.IsLive(Fixture.SecretClient, Legacy(1));
            FromRuntime = await _storage.TryGetFor(_eventStore, Namespace, _identifier);
        }
    }

    [Fact] void should_copy_the_key() => ctx.Report.Copied.ShouldEqual(1L);
    [Fact] void should_verify_the_copy() => ctx.Report.Verified.ShouldEqual(1L);
    [Fact] void should_write_the_canonical_secret() => ctx.CanonicalIsLive.ShouldBeTrue();
    [Fact] void should_keep_the_legacy_secret() => ctx.LegacyIsLive.ShouldBeTrue();
    [Fact] void should_let_the_runtime_storage_read_the_key() => ctx.FromRuntime!.Fingerprint.ShouldEqual(FirstKeyFingerprint);

    static string FirstKeyFingerprint => given.a_migration_on_the_emulator.FirstKey.Fingerprint;
}
