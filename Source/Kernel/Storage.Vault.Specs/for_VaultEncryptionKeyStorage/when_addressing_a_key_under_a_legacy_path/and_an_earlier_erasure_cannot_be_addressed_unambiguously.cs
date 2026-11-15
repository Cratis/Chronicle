// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_path.and_an_earlier_erasure_cannot_be_addressed_unambiguously.context;

namespace Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_path;

[Collection(VaultCollection.Name)]
public class and_an_earlier_erasure_cannot_be_addressed_unambiguously(context ctx) : IClassFixture<context>
{
    public class context(VaultFixture fixture) : given.a_vault_encryption_key_storage(fixture)
    {
        public Exception? ProvisionException;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            var raw = new given.raw_vault(Fixture.VaultAddress);
            await raw.Write($"{_eventStore}/default/a/b/erasure", new Dictionary<string, object> { ["erasedThrough"] = "1", ["erasedKeyFingerprints"] = string.Empty, ["newKeyAllowed"] = "False" });

            try
            {
                await _storage.GetOrAddFor(_eventStore, _namespace, "a/b", new EncryptionKey([7, 8, 9], [10, 11, 12]));
            }
            catch (Exception ex)
            {
                ProvisionException = ex;
            }
        }
    }

    [Fact] void should_refuse_provisioning() => ctx.ProvisionException.ShouldBeOfExactType<EncryptionKeyNeedsUpgrade>();
}
