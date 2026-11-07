// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_the_service_reports_a_disabled_secret;

public class and_the_earliest_candidate_is_disabled_and_a_later_one_is_enabled : given.a_storage_over_a_disabling_client
{
    Exception? _readError;
    Exception? _specificError;
    Exception? _provisionError;
    EncryptionKeyErasure? _erasure;
    int _writesBeforeReads;

    async Task Because()
    {
        Start("SecretDisabled", false);
        _client.Add(KeyName, "first", candidate: true, enabled: false);
        _client.Add(KeyName, "second", candidate: true);
        _writesBeforeReads = _client.Writes;
        _readError = await Catch.Exception(() => _storage.TryGetLatestFor(_eventStore, _namespace, Identifier));
        _specificError = await Catch.Exception(() => _storage.TryGetFor(_eventStore, _namespace, Identifier, (EncryptionKeyRevision)1u));
        _provisionError = await Catch.Exception(() => _storage.GetOrAddFor(_eventStore, _namespace, Identifier, Fresh));
        _writesBeforeReads = _client.Writes - _writesBeforeReads;
        await _storage.RecordErasureFor(_eventStore, _namespace, Identifier);
        await _storage.DeleteFor(_eventStore, _namespace, Identifier);
        _erasure = await _storage.GetErasureFor(_eventStore, _namespace, Identifier);
    }

    [Fact] void should_fail_the_latest_read_closed() => _readError.ShouldBeOfExactType<EncryptionKeyUnavailable>();
    [Fact] void should_fail_the_read_of_the_revision_closed() => _specificError.ShouldBeOfExactType<EncryptionKeyUnavailable>();
    [Fact] void should_fail_provisioning_closed() => _provisionError.ShouldBeOfExactType<EncryptionKeyUnavailable>();
    [Fact] void should_not_write_a_key_while_failing_closed() => _writesBeforeReads.ShouldEqual(0);
    [Fact] void should_cover_the_revision_when_erasing() => (_erasure!.ErasedThrough.Value >= 1u).ShouldBeTrue();
    [Fact] void should_purge_every_version_of_the_secret() => _client.Purged.ShouldContain(KeyName);
    [Fact] void should_leave_no_live_key() => _client.LiveNames.ShouldNotContain(KeyName);
}
