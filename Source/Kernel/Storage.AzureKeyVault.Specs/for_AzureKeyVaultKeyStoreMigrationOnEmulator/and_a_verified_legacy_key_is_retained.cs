// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_name;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultKeyStoreMigrationOnEmulator;

[Collection(AzureKeyVaultCollection.Name)]
public class and_a_verified_legacy_key_is_retained(and_a_verified_legacy_key_is_retained.context ctx) : IClassFixture<and_a_verified_legacy_key_is_retained.context>
{
    public class context(AzureKeyVaultFixture fixture) : given.a_migration_on_the_emulator(fixture)
    {
        public EncryptionKeyMigrationReport Report = default!;
        public EncryptionKeyStoreUpgradeStatus Status = default!;
        public bool LegacyIsLive;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            await WriteLegacyKey(1, FirstKey);
            await Migrate();
            Report = await Migrate(removeVerifiedLegacy: false);
            Status = await new AzureKeyVaultKeyStoreUpgradeCheck(_vault, _scopes).Inspect(true);
            LegacyIsLive = await LegacySecrets.IsLive(Fixture.SecretClient, Legacy(1));
        }
    }

    [Fact] void should_find_it_already_migrated() => ctx.Report.AlreadyMigrated.ShouldEqual(1L);
    [Fact] void should_not_remove_anything() => ctx.Report.LegacyRemoved.ShouldEqual(0L);
    [Fact] void should_keep_the_legacy_secret() => ctx.LegacyIsLive.ShouldBeTrue();
    [Fact] void should_count_it_as_retained() => ctx.Status.MigratedLegacyRetained.ShouldEqual(1L);
    [Fact] void should_not_count_it_as_pending() => ctx.Status.EarlierNameKeys.ShouldEqual(0L);
    [Fact] void should_not_need_the_upgrade() => ctx.Status.EarlierMaterialPresent.ShouldBeFalse();
}
