// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_name;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultKeyStoreMigrationOnEmulator;

[Collection(AzureKeyVaultCollection.Name)]
public class and_a_legacy_entry_carries_attributes(and_a_legacy_entry_carries_attributes.context ctx) : IClassFixture<and_a_legacy_entry_carries_attributes.context>
{
    public class context(AzureKeyVaultFixture fixture) : given.a_migration_on_the_emulator(fixture)
    {
        public EncryptionKeyMigrationReport Report = default!;
        public bool TaggedCopied;
        public bool DisabledCopied;
        public bool TaggedIsLive;
        public bool DisabledIsLive;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            await Write(1, FirstKey, tagged: true);
            await Write(2, SecondKey, tagged: false);
            Report = await Migrate(removeVerifiedLegacy: true);
            TaggedCopied = await LegacySecrets.IsLive(Fixture.SecretClient, Current(1));
            DisabledCopied = await LegacySecrets.IsLive(Fixture.SecretClient, Current(2));
            TaggedIsLive = await LegacySecrets.IsLive(Fixture.SecretClient, Legacy(1));
            DisabledIsLive = await LegacySecrets.IsLive(Fixture.SecretClient, Legacy(2));
        }

        async Task Write(uint revision, EncryptionKey key, bool tagged)
        {
            await WriteLegacyKey(revision, key);
            var properties = (await Fixture.SecretClient.GetSecretAsync(Legacy(revision))).Value.Properties;
            if (tagged)
            {
                properties.Tags["owner"] = "someone";
            }
            else
            {
                properties.Enabled = false;
            }

            await Fixture.SecretClient.UpdateSecretPropertiesAsync(properties);
        }
    }

    [Fact] void should_not_copy_the_tagged_entry() => ctx.TaggedCopied.ShouldBeFalse();
    [Fact] void should_not_copy_the_disabled_entry() => ctx.DisabledCopied.ShouldBeFalse();
    [Fact] void should_not_delete_the_tagged_entry() => ctx.TaggedIsLive.ShouldBeTrue();
    [Fact] void should_not_delete_the_disabled_entry() => ctx.DisabledIsLive.ShouldBeTrue();
    [Fact] void should_leave_the_entries_for_review() => ctx.Report.NeedsReview.ShouldBeTrue();
    [Fact] void should_not_remove_anything() => (ctx.Report.LegacyRemoved + ctx.Report.LegacyRemovedNotPurged).ShouldEqual(0L);
}
