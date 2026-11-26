// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_name;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultKeyStoreMigrationOnEmulator;

[Collection(AzureKeyVaultCollection.Name)]
public class and_an_erasure_record_exists(and_an_erasure_record_exists.context ctx) : IClassFixture<and_an_erasure_record_exists.context>
{
    public class context(AzureKeyVaultFixture fixture) : given.a_migration_on_the_emulator(fixture)
    {
        public EncryptionKeyMigrationReport Report = default!;
        public bool ErasureCopied;
        public bool ErasedKeyCopied;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            await WriteLegacyKey(1, FirstKey);
            await LegacySecrets.WriteErasure(Fixture.SecretClient, LegacyErasure, FirstKey);
            Report = await Migrate();
            ErasureCopied = await LegacySecrets.IsLive(Fixture.SecretClient, CurrentErasure);
            ErasedKeyCopied = await LegacySecrets.IsLive(Fixture.SecretClient, Current(1));
        }
    }

    [Fact] void should_copy_the_erasure_record() => ctx.ErasureCopied.ShouldBeTrue();
    [Fact] void should_count_the_erasure_record() => ctx.Report.ErasureRecordsCopied.ShouldEqual(1L);
    [Fact] void should_not_copy_the_erased_key() => ctx.ErasedKeyCopied.ShouldBeFalse();
}
