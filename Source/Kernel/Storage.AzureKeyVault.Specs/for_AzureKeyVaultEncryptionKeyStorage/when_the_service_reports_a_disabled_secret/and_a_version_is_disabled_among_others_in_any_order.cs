// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_the_service_reports_a_disabled_secret;

public class and_a_version_is_disabled_among_others_in_any_order : given.a_storage_over_a_disabling_client
{
    static readonly bool[][] _orders =
    [
        [false, true],
        [true, false, true],
        [true, true, false, true],
        [false, true, true]
    ];

    List<Exception?> _latestErrors = [];
    List<Exception?> _revisionErrors = [];
    List<bool> _purged = [];

    async Task Because()
    {
        foreach (var enabledVersions in _orders)
        {
            Start("SecretDisabled", false);
            foreach (var enabled in enabledVersions)
            {
                _client.Add(KeyName, "version", enabled: enabled);
            }

            _latestErrors.Add(await Catch.Exception(() => _storage.TryGetLatestFor(_eventStore, _namespace, Identifier)));
            _revisionErrors.Add(await Catch.Exception(() => _storage.TryGetFor(_eventStore, _namespace, Identifier, (EncryptionKeyRevision)1u)));
            await _storage.RecordErasureFor(_eventStore, _namespace, Identifier);
            await _storage.DeleteFor(_eventStore, _namespace, Identifier);
            _purged.Add(_client.Purged.Contains(KeyName) && !_client.LiveNames.Contains(KeyName));
        }
    }

    [Fact] void should_fail_every_latest_read_closed() => (_latestErrors.Count == _orders.Length && _latestErrors.TrueForAll(e => e is EncryptionKeyUnavailable)).ShouldBeTrue();
    [Fact] void should_fail_every_read_of_the_revision_closed() => (_revisionErrors.Count == _orders.Length && _revisionErrors.TrueForAll(e => e is EncryptionKeyUnavailable)).ShouldBeTrue();
    [Fact] void should_purge_every_secret_when_erasing() => _purged.TrueForAll(purged => purged).ShouldBeTrue();
}
