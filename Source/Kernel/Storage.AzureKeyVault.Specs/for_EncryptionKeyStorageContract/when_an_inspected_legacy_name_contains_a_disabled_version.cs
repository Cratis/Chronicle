// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_EncryptionKeyStorageContract;

public class when_an_inspected_legacy_name_contains_a_disabled_version : given.a_fenced_store
{
    Exception _error;

    async Task Establish()
    {
        var name = SecretLocations.For(EventStore, Namespace, Identifier).Name(1u);
        var disabled = (await Client.SetSecretAsync(name, LegacyAzureKeyVaultEncryptionKeyStorage.Serialize(Original))).Value.Properties;
        disabled.Enabled = false;
        await Client.UpdateSecretPropertiesAsync(disabled);
        await Client.SetSecretAsync(name, LegacyAzureKeyVaultEncryptionKeyStorage.Serialize(Successor));
    }

    async Task Because() => _error = await Catch.Exception(() => Storage.GetFor(EventStore, Namespace, Identifier, 1u));

    [Fact] void should_keep_the_legacy_disabled_version_check() => _error.ShouldBeOfExactType<EncryptionKeyUnavailable>();
}
