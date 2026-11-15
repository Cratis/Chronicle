// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Storage.Vault.for_live_VaultForwardMigration;

[Collection(LiveVaultCollection.Name)]
public class when_a_version_has_scheduled_deletion(LiveVaultFixture fixture) : given.a_live_migration(fixture)
{
    bool _liveBeforeDeadline;
    string _sourceDeletion = default!;
    bool _copiedWithoutDeadlineOrRefused;

    async Task Establish()
    {
        await Post($"{_mount}/metadata/{Legacy}", new { delete_version_after = "1h" });
        await Put(Legacy);
        _sourceDeletion = (await Metadata(Legacy))!["versions"]!["1"]!["deletion_time"]!.GetValue<string>();
        (DateTimeOffset.Parse(_sourceDeletion, CultureInfo.InvariantCulture) > DateTimeOffset.UtcNow).ShouldBeTrue();
        _liveBeforeDeadline = await VersionHasPayload(Legacy, 1, Key()) && !await _store.IsTombstone(Legacy);
    }

    async Task Because()
    {
        var report = await Migrate();
        var copy = await _store.Read(Current);
        var destinationDeletion = (await Metadata(Current))?["versions"]?["1"]?["deletion_time"]?.GetValue<string>();
        _copiedWithoutDeadlineOrRefused = copy is null
            ? report.NeedsReview || report.Failed > 0 || report.LayoutUnusable
            : string.IsNullOrEmpty(destinationDeletion) || destinationDeletion == _sourceDeletion;
    }

    [Fact] void should_recognize_a_future_deletion_as_still_live() => _liveBeforeDeadline.ShouldBeTrue();

    [Fact]
    void should_copy_the_value_and_leave_the_hand_set_deletion_deadline_behind()
    {
        // Documented limitation (independent design check, advisory): only the key value is copied; per-key
        // metadata set by hand outside Chronicle is not. Preserved deadline or explicit refusal also satisfies this.
        _copiedWithoutDeadlineOrRefused.ShouldBeTrue();
    }
}
