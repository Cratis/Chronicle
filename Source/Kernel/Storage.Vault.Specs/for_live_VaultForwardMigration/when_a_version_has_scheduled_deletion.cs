// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Storage.Vault.for_live_VaultForwardMigration;

[Collection(LiveVaultCollection.Name)]
public class when_a_version_has_scheduled_deletion(LiveVaultFixture fixture) : given.a_live_migration(fixture)
{
    bool _liveBeforeDeadline;
    string _sourceDeletion = default!;
    bool _preservedOrRefused;

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
        _preservedOrRefused = copy is null
            ? report.NeedsReview || report.Failed > 0 || report.LayoutUnusable
            : destinationDeletion == _sourceDeletion;
    }

    [Fact] void should_recognize_a_future_deletion_as_still_live() => _liveBeforeDeadline.ShouldBeTrue();

    [Fact]
    void should_preserve_the_deletion_deadline_or_explicitly_refuse_the_copy()
    {
        // Hostile security assertion, not a new production contract. See README.md: preservation/refusal
        // is unresolved, and this assertion is expected to expose the unsupported case rather than skip it.
        _preservedOrRefused.ShouldBeTrue();
    }
}
