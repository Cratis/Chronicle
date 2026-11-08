// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_name.and_the_identifier_is_not_canonical.context;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_name;

[Collection(AzureKeyVaultCollection.Name)]
public class and_the_identifier_is_not_canonical(context ctx) : IClassFixture<context>
{
    public class context(AzureKeyVaultFixture fixture) : given.a_azure_key_vault_encryption_key_storage(fixture)
    {
        public Exception? LatestError;
        public Exception? ProvisionError;
        public bool NewKeyWritten;
        public bool LegacyIsLive;
        public bool ErasureRecorded;
        public EncryptionKeyAtRevision? LatestAfterErasure;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            var legacyName = LegacySecrets.Name(_eventStore, "alice-x", "1");
            await LegacySecrets.WriteKey(Fixture.SecretClient, legacyName, new EncryptionKey([1, 2, 3], [4, 5, 6]));
            var locations = SecretLocations.For(_eventStore, _namespace, "Alice.X");

            LatestError = await Catch.Exception(() => _storage.TryGetLatestFor(_eventStore, _namespace, "Alice.X"));
            ProvisionError = await Catch.Exception(() => _storage.GetOrAddFor(_eventStore, _namespace, "Alice.X", new EncryptionKey([7, 8, 9], [10, 11, 12])));
            NewKeyWritten = await LegacySecrets.IsLive(Fixture.SecretClient, locations.Name((EncryptionKeyRevision)1u));

            await _storage.RecordErasureFor(_eventStore, _namespace, "Alice.X");
            await _storage.DeleteFor(_eventStore, _namespace, "Alice.X");
            ErasureRecorded = await LegacySecrets.IsLive(Fixture.SecretClient, locations.ErasureName);
            LegacyIsLive = await LegacySecrets.IsLive(Fixture.SecretClient, legacyName);
            LatestAfterErasure = await _storage.TryGetLatestFor(_eventStore, _namespace, "Alice.X");
        }
    }

    [Fact] void should_refuse_reading() => ctx.LatestError.ShouldBeOfExactType<EncryptionKeyNeedsUpgrade>();
    [Fact] void should_refuse_provisioning() => ctx.ProvisionError.ShouldBeOfExactType<EncryptionKeyNeedsUpgrade>();
    [Fact] void should_not_put_the_identifier_in_the_message() => ctx.LatestError!.Message.ShouldNotContain("Alice");
    [Fact] void should_not_write_a_new_key() => ctx.NewKeyWritten.ShouldBeFalse();
    [Fact] void should_record_the_erasure_at_the_new_location() => ctx.ErasureRecorded.ShouldBeTrue();
    [Fact] void should_not_remove_the_earlier_secret() => ctx.LegacyIsLive.ShouldBeTrue();
    [Fact] void should_read_as_empty_after_the_erasure() => ctx.LatestAfterErasure.ShouldBeNull();
}
