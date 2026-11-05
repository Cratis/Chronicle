// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_the_service_reports_a_disabled_secret.given;

/// <summary>
/// A storage over a <see cref="DisablingSecretClient"/> holding one key.
/// </summary>
public class a_storage_over_a_disabling_client : Specification
{
    protected const string Identifier = "customer-42";

    protected static readonly EncryptionKey Key = new([1, 2, 3], [4, 5, 6]);
    protected static readonly EncryptionKey Fresh = new([7, 8, 9], [10, 11, 12]);

    protected DisablingSecretClient _client = default!;
    protected AzureKeyVaultEncryptionKeyStorage _storage = default!;
    protected EventStoreName _eventStore = "store";
    protected EventStoreNamespaceName _namespace = "default";

    protected string KeyName => SecretLocations.For(_eventStore, _namespace, Identifier).Name((EncryptionKeyRevision)1u);

    protected string ErasureName => SecretLocations.For(_eventStore, _namespace, Identifier).ErasureName;

    protected async Task Seed(string errorCode, bool listingShowsDisabled)
    {
        _client = new DisablingSecretClient(errorCode, listingShowsDisabled);
        _storage = new AzureKeyVaultEncryptionKeyStorage(_client, TimeProvider.System);
        await _storage.SaveFor(_eventStore, _namespace, Identifier, Key);
    }
}
