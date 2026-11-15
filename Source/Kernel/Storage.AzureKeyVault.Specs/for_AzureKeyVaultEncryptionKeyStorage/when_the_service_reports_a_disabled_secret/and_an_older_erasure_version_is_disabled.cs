// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_the_service_reports_a_disabled_secret;

public class and_an_older_erasure_version_is_disabled : given.a_storage_over_a_disabling_client
{
    Exception? _readError;
    Exception? _provisionError;
    Exception? _recordError;
    int _writes;

    async Task Because()
    {
        await Seed("SecretDisabled", false);
        await _storage.RecordErasureFor(_eventStore, _namespace, Identifier);
        await _storage.RecordErasureFor(_eventStore, _namespace, Identifier);
        await _storage.DeleteFor(_eventStore, _namespace, Identifier);
        _client.DisableVersion(ErasureName, 0);
        _writes = _client.Writes;
        _readError = await Catch.Exception(() => _storage.GetErasureFor(_eventStore, _namespace, Identifier));
        _provisionError = await Catch.Exception(() => _storage.GetOrAddFor(_eventStore, _namespace, Identifier, Fresh));
        _recordError = await Catch.Exception(() => _storage.RecordErasureFor(_eventStore, _namespace, Identifier));
    }

    [Fact] void should_fail_the_erasure_read_closed() => _readError.ShouldBeOfExactType<EncryptionKeyUnavailable>();
    [Fact] void should_fail_provisioning_closed() => _provisionError.ShouldBeOfExactType<EncryptionKeyUnavailable>();
    [Fact] void should_fail_recording_another_erasure_closed() => _recordError.ShouldBeOfExactType<EncryptionKeyUnavailable>();
    [Fact] void should_not_write_anything() => _client.Writes.ShouldEqual(_writes);
}
