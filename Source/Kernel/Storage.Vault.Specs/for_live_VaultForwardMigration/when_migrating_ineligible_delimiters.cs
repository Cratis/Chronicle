// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_live_VaultForwardMigration;

[Collection(LiveVaultCollection.Name)]
public class when_migrating_ineligible_delimiters(LiveVaultFixture fixture) : given.a_live_migration(fixture)
{
    EncryptionKeyMigrationReport _report = default!;

    async Task Establish()
    {
        await Put("store/default/id/with/slash/1");
        await Put("store/default/id?query/1");
        await Put("store/default/id#fragment/1");
    }

    async Task Because() => _report = await Migrate(remove: true);

    [Fact] void should_require_review_instead_of_guessing_identity() => _report.AmbiguousForReview.ShouldEqual(3L);
    [Fact] void should_not_copy_ambiguous_material() => _report.Copied.ShouldEqual(0L);
    [Fact] void should_not_delete_ambiguous_material() => _report.LegacyRemoved.ShouldEqual(0L);
    [Fact] async Task should_keep_the_source() => (await HasPayload("store/default/id/with/slash/1", Key())).ShouldBeTrue();
}
