// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_deleting_a_key;

[Collection(AzureKeyVaultCollection.Name)]
public class and_it_must_be_purged(and_it_must_be_purged.context ctx) : IClassFixture<and_it_must_be_purged.context>
{
    public class context(AzureKeyVaultFixture fixture) : given.a_azure_key_vault_encryption_key_storage(fixture)
    {
        public List<string> RecoverableSecrets = [];

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            EncryptionKeyIdentifier identifier = Guid.NewGuid().ToString();
            await _storage.SaveFor(_eventStore, _namespace, identifier, new EncryptionKey([1, 2, 3], [4, 5, 6]));
            await _storage.SaveFor(_eventStore, _namespace, identifier, new EncryptionKey([7, 8, 9], [10, 11, 12]));
            await _storage.DeleteFor(_eventStore, _namespace, identifier);

            await foreach (var deleted in Fixture.SecretClient.GetDeletedSecretsAsync())
            {
                if (deleted.Name.Contains(identifier.Value, StringComparison.OrdinalIgnoreCase) && !deleted.Name.Contains("erasure", StringComparison.OrdinalIgnoreCase))
                {
                    RecoverableSecrets.Add(deleted.Name);
                }
            }
        }
    }

    [Fact] void should_leave_no_recoverable_key_material() => ctx.RecoverableSecrets.ShouldBeEmpty();
}
