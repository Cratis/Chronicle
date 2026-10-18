// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_getting_the_latest_key_with_its_revision;

[Collection(AzureKeyVaultCollection.Name)]
public class and_there_is_no_key(AzureKeyVaultFixture fixture) : Specification
{
    IEncryptionKeyStorage _store;
    EncryptionKeyIdentifier _identifier = Guid.NewGuid().ToString();
    EncryptionKeyAtRevision? _result;

    void Establish()
    {
        _store = new AzureKeyVaultEncryptionKeyStorage(fixture.SecretClient);
    }

    async Task Because() => _result = await _store.TryGetLatestFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);

    [Fact] void should_report_absence() => _result.ShouldBeNull();
    [Fact] async Task should_not_provision_a_key() => (await _store.HasFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier)).ShouldBeFalse();
}
