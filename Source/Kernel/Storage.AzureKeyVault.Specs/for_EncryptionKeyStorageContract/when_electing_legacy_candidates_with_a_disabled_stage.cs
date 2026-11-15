// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Security.KeyVault.Secrets;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_EncryptionKeyStorageContract;

public class when_electing_legacy_candidates_with_a_disabled_stage : given.a_fenced_store
{
    EncryptionKey _read;

    async Task Establish()
    {
        var name = SecretLocations.For(EventStore, Namespace, Identifier).Name(1u);
        var stage = new KeyVaultSecret(name, LegacyAzureKeyVaultEncryptionKeyStorage.Serialize(Successor));
        ProvisioningCandidateElection.MarkAsCandidate(stage);
        stage.Properties.Tags[FencedAzureKeyVaultEncryptionKeyStorage.StagedLifecycleTag] = "1";
        var staged = (await Client.SetSecretAsync(stage)).Value.Properties;
        staged.Enabled = false;
        await Client.UpdateSecretPropertiesAsync(staged);
        var first = new KeyVaultSecret(name, LegacyAzureKeyVaultEncryptionKeyStorage.Serialize(Original));
        ProvisioningCandidateElection.MarkAsCandidate(first);
        await Client.SetSecretAsync(first);
        var later = new KeyVaultSecret(name, LegacyAzureKeyVaultEncryptionKeyStorage.Serialize(Successor));
        ProvisioningCandidateElection.MarkAsCandidate(later);
        await Client.SetSecretAsync(later);
    }

    async Task Because() => _read = await Storage.GetFor(EventStore, Namespace, Identifier, 1u);

    [Fact] void should_elect_the_oldest_legacy_candidate_without_considering_the_disabled_stage() => _read.Private.ShouldEqual(Original.Private);
}
