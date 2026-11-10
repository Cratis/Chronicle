// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreMigrator;

public class when_a_fence_changes_between_verification_and_deletion : given.a_migrator
{
    EncryptionKeyMigrationReport _report;

    void Establish()
    {
        _store.Put($"{Address}/1", Key(1));
        _store.BeforeRemove = path =>
        {
            // The source and destination have already compared equal in the migrator.
            if (path == $"{Address}/1")
            {
                _store.Put($"{Current}/erasure", new Dictionary<string, string> { ["erasedThrough"] = "1" });
            }
        };
    }

    async Task Because() => _report = await Migrate(remove: true);

    [Fact] void should_skip_deleting_the_source() => _store.Has($"{Address}/1").ShouldBeTrue();
    [Fact] void should_report_the_interference() => _report.ConflictsKept.ShouldEqual(1L);
    [Fact] void should_report_no_legacy_removals() => _report.LegacyRemoved.ShouldEqual(0L);
}
