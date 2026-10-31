// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_provisioning_a_key;

[Collection(AzureKeyVaultCollection.Name)]
public class and_provisioners_race_for_it(and_provisioners_race_for_it.context ctx) : IClassFixture<and_provisioners_race_for_it.context>
{
    public class context(AzureKeyVaultFixture fixture) : given.a_azure_key_vault_encryption_key_storage(fixture)
    {
        const int Provisioners = 8;

        public EncryptionKeyAtRevision[] Results = [];
        public EncryptionKeyAtRevision? Stored;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            EncryptionKeyIdentifier identifier = Guid.NewGuid().ToString();
            Results = await Task.WhenAll(Enumerable.Range(0, Provisioners).Select(provisioner =>
                _storage.GetOrAddWithRevisionFor(_eventStore, _namespace, identifier, new EncryptionKey([(byte)provisioner], [(byte)(provisioner + 100)]))));
            Stored = await _storage.TryGetLatestFor(_eventStore, _namespace, identifier);
        }
    }

    [Fact] void should_hand_every_provisioner_the_same_key() => ctx.Results.Select(_ => _.Key.Fingerprint).Distinct().Count().ShouldEqual(1);
    [Fact] void should_hand_every_provisioner_the_key_that_is_served() => ctx.Results[0].Key.Fingerprint.ShouldEqual(ctx.Stored!.Key.Fingerprint);
    [Fact] void should_hand_every_provisioner_the_served_private_key() => ctx.Results.All(_ => _.Key.Private.SequenceEqual(ctx.Stored!.Key.Private)).ShouldBeTrue();
    [Fact] void should_hand_every_provisioner_the_initial_revision() => ctx.Results.All(_ => _.Revision == EncryptionKeyRevision.Initial).ShouldBeTrue();
}
