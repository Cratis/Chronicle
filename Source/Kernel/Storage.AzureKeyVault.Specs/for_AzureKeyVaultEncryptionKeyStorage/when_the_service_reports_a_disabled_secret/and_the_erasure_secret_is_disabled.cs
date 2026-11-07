// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_the_service_reports_a_disabled_secret;

public class and_the_erasure_secret_is_disabled : given.a_storage_over_a_disabling_client
{
    Exception?[] _errors = [];
    int _writes;

    async Task Because()
    {
        _errors = new Exception?[4];
        var index = 0;
        foreach (var errorCode in new[] { "SecretDisabled", "Forbidden" })
        {
            await Seed(errorCode, false);
            await _storage.RecordErasureFor(_eventStore, _namespace, Identifier);
            await _storage.DeleteFor(_eventStore, _namespace, Identifier);
            _client.Disable(ErasureName);
            _writes = _client.Writes;
            _errors[index++] = await Catch.Exception(() => _storage.GetErasureFor(_eventStore, _namespace, Identifier));
            _errors[index++] = await Catch.Exception(() => _storage.GetOrAddFor(_eventStore, _namespace, Identifier, Fresh));
        }
    }

    [Fact] void should_fail_the_erasure_read_closed() => _errors[0].ShouldBeOfExactType<EncryptionKeyUnavailable>();
    [Fact] void should_fail_provisioning_closed() => _errors[1].ShouldBeOfExactType<EncryptionKeyUnavailable>();
    [Fact] void should_fail_the_erasure_read_closed_for_the_other_error_code() => _errors[2].ShouldBeOfExactType<EncryptionKeyUnavailable>();
    [Fact] void should_fail_provisioning_closed_for_the_other_error_code() => _errors[3].ShouldBeOfExactType<EncryptionKeyUnavailable>();
    [Fact] void should_not_write_a_new_key() => _client.Writes.ShouldEqual(_writes);
}
