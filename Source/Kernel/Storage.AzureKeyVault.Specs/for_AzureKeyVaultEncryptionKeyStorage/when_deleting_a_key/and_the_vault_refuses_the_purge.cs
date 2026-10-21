// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure;
using Azure.Security.KeyVault.Secrets;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_deleting_a_key;

public class and_the_vault_refuses_the_purge : Specification
{
    SecretClient _secretClient;
    AzureKeyVaultEncryptionKeyStorage _storage;
    Exception _error;

    void Establish()
    {
        _secretClient = Substitute.For<SecretClient>();
        _secretClient.StartDeleteSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Substitute.For<DeleteSecretOperation>());
        _secretClient.PurgeDeletedSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<Response>>(_ => throw new RequestFailedException(403, "Operation \"purge\" is not allowed because purge protection is enabled for this vault."));
        _storage = new AzureKeyVaultEncryptionKeyStorage(_secretClient);
    }

    async Task Because() => _error = await Catch.Exception(() =>
        _storage.DeleteFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "some-identifier", EncryptionKeyRevision.Initial));

    [Fact] void should_fail_the_deletion_as_not_purged() => _error.ShouldBeOfExactType<EncryptionKeyNotPurged>();
    [Fact] void should_have_soft_deleted_the_key_first() => _secretClient.Received(1).StartDeleteSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
}
