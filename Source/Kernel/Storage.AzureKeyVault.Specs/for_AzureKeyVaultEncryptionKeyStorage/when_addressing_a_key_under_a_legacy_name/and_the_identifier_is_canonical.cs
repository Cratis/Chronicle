// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_name.and_the_identifier_is_canonical.context;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_name;

[Collection(AzureKeyVaultCollection.Name)]
public class and_the_identifier_is_canonical(context ctx) : IClassFixture<context>
{
    public class context(AzureKeyVaultFixture fixture) : given.a_azure_key_vault_encryption_key_storage(fixture)
    {
        public EncryptionKey LegacyKey = new([1, 2, 3], [4, 5, 6]);
        public EncryptionKey NewKey = new([7, 8, 9], [10, 11, 12]);
        public EncryptionKeyAtRevision? Latest;
        public EncryptionKey? Provisioned;
        public EncryptionKey? FirstRevision;
        public EncryptionKey? SecondRevision;
        public bool SecondIsStoredUnderTheNewName;
        public bool SecondIsStoredUnderAnEarlierName;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            EncryptionKeyIdentifier identifier = "customer-42";
            await LegacySecrets.WriteKey(Fixture.SecretClient, LegacySecrets.Name(_eventStore, identifier, "1"), LegacyKey);

            Latest = await _storage.TryGetLatestFor(_eventStore, _namespace, identifier);
            Provisioned = await _storage.GetOrAddFor(_eventStore, _namespace, identifier, new EncryptionKey([9, 9], [9, 9]));

            await _storage.SaveFor(_eventStore, _namespace, identifier, NewKey);
            FirstRevision = await _storage.TryGetFor(_eventStore, _namespace, identifier, 1u);
            SecondRevision = await _storage.TryGetFor(_eventStore, _namespace, identifier, 2u);
            SecondIsStoredUnderTheNewName = await LegacySecrets.IsLive(Fixture.SecretClient, SecretLocations.For(_eventStore, _namespace, identifier).Name(2u));
            SecondIsStoredUnderAnEarlierName = await LegacySecrets.IsLive(Fixture.SecretClient, LegacySecrets.Name(_eventStore, identifier, "2"));
        }
    }

    [Fact] void should_read_the_earlier_key_as_the_latest() => ctx.Latest!.Key.Fingerprint.ShouldEqual(ctx.LegacyKey.Fingerprint);
    [Fact] void should_read_the_earlier_key_at_revision_one() => ctx.Latest!.Revision.Value.ShouldEqual(1u);
    [Fact] void should_return_the_earlier_key_instead_of_provisioning() => ctx.Provisioned!.Fingerprint.ShouldEqual(ctx.LegacyKey.Fingerprint);
    [Fact] void should_still_read_the_earlier_key_by_revision() => ctx.FirstRevision!.Fingerprint.ShouldEqual(ctx.LegacyKey.Fingerprint);
    [Fact] void should_read_the_saved_key_by_revision() => ctx.SecondRevision!.Fingerprint.ShouldEqual(ctx.NewKey.Fingerprint);
    [Fact] void should_save_the_next_revision_after_the_earlier_one() => ctx.SecondIsStoredUnderTheNewName.ShouldBeTrue();
    [Fact] void should_not_write_under_an_earlier_name() => ctx.SecondIsStoredUnderAnEarlierName.ShouldBeFalse();
}
