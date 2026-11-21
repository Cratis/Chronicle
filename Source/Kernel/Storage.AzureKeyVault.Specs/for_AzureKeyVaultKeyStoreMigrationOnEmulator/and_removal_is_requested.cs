// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_name;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultKeyStoreMigrationOnEmulator;

[Collection(AzureKeyVaultCollection.Name)]
public class and_removal_is_requested(and_removal_is_requested.context ctx) : IClassFixture<and_removal_is_requested.context>
{
    public class context(AzureKeyVaultFixture fixture) : given.a_migration_on_the_emulator(fixture)
    {
        public EncryptionKeyMigrationReport Report = default!;
        public bool LegacyIsLive;
        public bool LegacyIsRecoverable;
        public EncryptionKey? FromRuntime;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            await WriteLegacyKey(1, FirstKey);
            Report = await Migrate(removeVerifiedLegacy: true);
            LegacyIsLive = await LegacySecrets.IsLive(Fixture.SecretClient, Legacy(1));
            LegacyIsRecoverable = await LegacySecrets.IsSoftDeleted(Fixture.SecretClient, Legacy(1));
            FromRuntime = await _storage.TryGetFor(_eventStore, Namespace, _identifier);
        }
    }

    [Fact] void should_remove_the_legacy_secret() => ctx.LegacyIsLive.ShouldBeFalse();
    [Fact] void should_account_for_the_removal_either_way() => (ctx.Report.LegacyRemoved + ctx.Report.LegacyRemovedNotPurged).ShouldEqual(1L);
    [Fact] void should_report_the_purge_outcome_consistently() => (ctx.LegacyIsRecoverable ? ctx.Report.LegacyRemovedNotPurged : ctx.Report.LegacyRemoved).ShouldEqual(1L);
    [Fact] void should_keep_the_key_readable_at_the_canonical_name() => ctx.FromRuntime!.Fingerprint.ShouldEqual(given.a_migration_on_the_emulator.FirstKey.Fingerprint);
}
