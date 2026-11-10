// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreMigrator;

public class when_dry_running : given.a_migrator
{
    EncryptionKeyMigrationReport _report;

    void Establish()
    {
        _store.Put($"{Address}/erasure", new Dictionary<string, string> { ["erasedThrough"] = "0" });
        _store.Put($"{Address}/1", Key(1));
        _store.Put($"{Address}/2", Key(2));
    }

    async Task Because() => _report = await Migrate(dryRun: true);

    [Fact] void should_not_write_anything() => _store.Writes.ShouldEqual(0);
    [Fact] void should_not_remove_anything() => _store.Removes.ShouldEqual(0);
    [Fact] void should_not_create_anything_under_the_current_names() => _store.Paths.Any(path => path.StartsWith("k1/", StringComparison.Ordinal) || path == VaultPathSegments.LayoutMarker).ShouldBeFalse();
    [Fact] void should_be_a_dry_run() => _report.DryRun.ShouldBeTrue();
    [Fact] void should_count_the_keys_it_would_copy() => _report.Copied.ShouldEqual(2L);
    [Fact] void should_count_the_erasure_records_it_would_copy() => _report.ErasureRecordsCopied.ShouldEqual(1L);
    [Fact] void should_not_verify_anything() => _report.Verified.ShouldEqual(0L);
}
