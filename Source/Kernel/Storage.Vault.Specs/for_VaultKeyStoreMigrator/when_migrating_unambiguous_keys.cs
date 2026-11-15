// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreMigrator;

public class when_migrating_unambiguous_keys : given.a_migrator
{
    EncryptionKeyMigrationReport _report;

    void Establish()
    {
        _store.Put($"{Address}/1", Key(1));
        _store.Put($"{Address}/2", Key(2));
    }

    async Task Because() => _report = await Migrate();

    [Fact] void should_copy_each_revision_as_it_is() => _store.DataOf($"{Current}/2").ShouldEqual(WithLayout(Key(2)));
    [Fact] void should_copy_the_first_revision() => _store.DataOf($"{Current}/1").ShouldEqual(WithLayout(Key(1)));
    [Fact] void should_keep_the_keys_under_earlier_names() => _store.Has($"{Address}/1").ShouldBeTrue();
    [Fact] void should_record_the_layout() => _store.Has(VaultPathSegments.LayoutMarker).ShouldBeTrue();
    [Fact] void should_count_what_was_scanned() => _report.Scanned.ShouldEqual(2L);
    [Fact] void should_count_the_copies() => _report.Copied.ShouldEqual(2L);
    [Fact] void should_verify_the_copies() => _report.Verified.ShouldEqual(2L);
    [Fact] void should_not_remove_anything() => _report.LegacyRemoved.ShouldEqual(0L);
    [Fact] void should_not_fail() => _report.Failed.ShouldEqual(0L);
    [Fact] void should_be_complete() => _report.Complete.ShouldBeTrue();
    [Fact] void should_leave_nothing_for_review() => _report.NeedsReview.ShouldBeFalse();
}
