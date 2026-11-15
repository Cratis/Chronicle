// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_a_key_secret_is_disabled.and_saving_latest.context;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_a_key_secret_is_disabled;

[Collection(AzureKeyVaultCollection.Name)]
public class and_saving_latest(context ctx) : IClassFixture<context>
{
    public class context(AzureKeyVaultFixture fixture) : given.a_azure_key_vault_encryption_key_storage(fixture)
    {
        const string Identifier = "customer-42";

        public EncryptionKey ExistingKey = new([1, 2, 3], [4, 5, 6]);
        public EncryptionKey FreshKey = new([7, 8, 9], [10, 11, 12]);
        public bool SecondRevisionExists;
        public int Versions;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            var locations = SecretLocations.For(_eventStore, _namespace, Identifier);
            var keyName = locations.Name((EncryptionKeyRevision)1u);
            await _storage.SaveFor(_eventStore, _namespace, Identifier, ExistingKey);
            await DisabledSecrets.Disable(Fixture.SecretClient, keyName);
            await _storage.SaveFor(_eventStore, _namespace, Identifier, FreshKey);
            Versions = await DisabledSecrets.VersionCount(Fixture.SecretClient, keyName);
            SecondRevisionExists = await for_AzureKeyVaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_name.LegacySecrets.IsLive(Fixture.SecretClient, locations.Name((EncryptionKeyRevision)2u));
        }
    }

    [Fact] void should_not_reuse_the_disabled_revision() => ctx.Versions.ShouldEqual(2);
    [Fact] void should_save_the_next_revision() => ctx.SecondRevisionExists.ShouldBeTrue();
}
