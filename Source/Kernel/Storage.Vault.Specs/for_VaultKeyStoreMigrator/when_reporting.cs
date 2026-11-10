// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreMigrator;

public class when_reporting : given.a_migrator
{
    const string SentinelStore = "sentinel-store-7f3a";
    const string SentinelIdentifier = "sentinel-identifier-91c2";

    given.RecordingLogger<VaultKeyStoreMigrator> _logger;
    EncryptionKeyMigrationReport _report;
    string _privateKey;

    void Establish()
    {
        _logger = new();
        _migrator = new VaultKeyStoreMigrator(_store, _logger);
        var key = Key(1, privateByte: 42);
        _privateKey = key["privateKey"];
        _store.Put($"{SentinelStore}/default/{SentinelIdentifier}/1", key);
        _store.Put($"{SentinelStore}/default/{SentinelIdentifier}/2", Key(2, privateByte: 43));

        // The failure names the path, the way an answer from the key store could.
        _store.FailOnWrite = 3;
    }

    async Task Because() => _report = await Migrate();

    [Fact] void should_log_the_failure() => _logger.Messages.ShouldNotBeEmpty();
    [Fact] void should_not_log_the_event_store() => _logger.Messages.Exists(message => message.Contains(SentinelStore, StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_not_log_the_identifier() => _logger.Messages.Exists(message => message.Contains(SentinelIdentifier, StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_not_log_key_material() => _logger.Messages.Exists(message => message.Contains(_privateKey, StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_not_hold_the_event_store_in_the_report() => _report.ToString().Contains(SentinelStore, StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_hold_the_identifier_in_the_report() => _report.ToString().Contains(SentinelIdentifier, StringComparison.Ordinal).ShouldBeFalse();
}
