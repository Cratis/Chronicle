// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_path.and_the_identifier_has_no_earlier_material.context;

namespace Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_path;

[Collection(VaultCollection.Name)]
public class and_the_identifier_has_no_earlier_material(context ctx) : IClassFixture<context>
{
    public class context(VaultFixture fixture) : given.a_vault_encryption_key_storage(fixture)
    {
        public EncryptionKey FreshKey = new([7, 8, 9], [10, 11, 12]);
        public EncryptionKey? Provisioned;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            Provisioned = await _storage.GetOrAddFor(_eventStore, _namespace, "a/b", FreshKey);
        }
    }

    [Fact] void should_provision_the_given_key() => ctx.Provisioned!.Fingerprint.ShouldEqual(ctx.FreshKey.Fingerprint);
}
