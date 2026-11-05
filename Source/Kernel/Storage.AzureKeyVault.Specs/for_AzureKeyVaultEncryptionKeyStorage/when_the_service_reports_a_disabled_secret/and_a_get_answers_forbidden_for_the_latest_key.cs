// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_the_service_reports_a_disabled_secret;

public class and_a_get_answers_forbidden_for_the_latest_key : given.a_storage_over_a_disabling_client
{
    Exception?[] _errors = [];
    int _writes;
    bool _hasFirstRevision;

    async Task Because()
    {
        _errors = new Exception?[4];
        var index = 0;
        foreach (var errorCode in new[] { "SecretDisabled", "Forbidden" })
        {
            await Seed(errorCode, false);
            _client.Disable(KeyName);
            _writes = _client.Writes;
            _errors[index++] = await Catch.Exception(() => _storage.TryGetLatestFor(_eventStore, _namespace, Identifier));
            _errors[index++] = await Catch.Exception(() => _storage.GetOrAddFor(_eventStore, _namespace, Identifier, Fresh));
            _hasFirstRevision = await _storage.HasFor(_eventStore, _namespace, Identifier, (EncryptionKeyRevision)1u);
        }
    }

    [Fact] void should_fail_the_latest_read_closed() => _errors[0].ShouldBeOfExactType<EncryptionKeyUnavailable>();
    [Fact] void should_fail_provisioning_closed() => _errors[1].ShouldBeOfExactType<EncryptionKeyUnavailable>();
    [Fact] void should_fail_the_read_closed_for_the_other_error_code() => _errors[2].ShouldBeOfExactType<EncryptionKeyUnavailable>();
    [Fact] void should_fail_provisioning_closed_for_the_other_error_code() => _errors[3].ShouldBeOfExactType<EncryptionKeyUnavailable>();
    [Fact] void should_not_write_a_new_key() => _client.Writes.ShouldEqual(_writes);
    [Fact] void should_still_report_the_revision_as_present() => _hasFirstRevision.ShouldBeTrue();
}
