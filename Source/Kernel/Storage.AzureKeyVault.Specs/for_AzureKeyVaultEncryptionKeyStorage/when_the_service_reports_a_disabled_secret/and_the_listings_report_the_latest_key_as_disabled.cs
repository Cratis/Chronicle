// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_the_service_reports_a_disabled_secret;

public class and_the_listings_report_the_latest_key_as_disabled : given.a_storage_over_a_disabling_client
{
    Exception? _readError;
    Exception? _provisionError;
    int _writes;

    async Task Because()
    {
        await Seed("SecretDisabled", true);
        _client.Disable(KeyName);
        _writes = _client.Writes;
        _readError = await Catch.Exception(() => _storage.TryGetLatestFor(_eventStore, _namespace, Identifier));
        _provisionError = await Catch.Exception(() => _storage.GetOrAddFor(_eventStore, _namespace, Identifier, Fresh));
    }

    [Fact] void should_fail_the_read_closed() => _readError.ShouldBeOfExactType<EncryptionKeyUnavailable>();
    [Fact] void should_fail_provisioning_closed() => _provisionError.ShouldBeOfExactType<EncryptionKeyUnavailable>();
    [Fact] void should_not_write_a_new_key() => _client.Writes.ShouldEqual(_writes);
}
