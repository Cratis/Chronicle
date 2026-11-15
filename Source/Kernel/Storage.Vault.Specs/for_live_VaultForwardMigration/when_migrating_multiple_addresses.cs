// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_live_VaultForwardMigration;

[Collection(LiveVaultCollection.Name)]
public class when_migrating_multiple_addresses(LiveVaultFixture fixture) : given.a_live_migration(fixture)
{
    readonly string[] _sources = [Legacy, "store/other/customer-43/2", "other/default/customer-44/3", "støre/租户/id~@+:,$/1"];
    readonly string[] _destinations = [Current, "k1/store/other/customer-43/2", "k1/other/default/customer-44/3", "k1/st~C3~B8re/~E7~A7~9F~E6~88~B7/id~7E@+:,$/1"];
    EncryptionKeyMigrationReport _report = default!;

    async Task Establish()
    {
        foreach (var path in _sources)
        {
            await Put(path);
        }
    }

    async Task Because() => _report = await Migrate();

    [Fact] void should_verify_every_address() => _report.Verified.ShouldEqual(4L);
    [Fact] void should_finish_without_review() => (_report.Complete && !_report.NeedsReview && _report.Failed == 0).ShouldBeTrue();

    [Fact]
    async Task should_preserve_payloads_at_the_exact_encoded_paths()
    {
        foreach (var path in _destinations)
        {
            (await HasPayload(path, WithLayout(Key()))).ShouldBeTrue();
        }
    }

    [Fact]
    async Task should_keep_every_legacy_payload_until_explicit_cleanup()
    {
        foreach (var path in _sources)
        {
            (await HasPayload(path, Key())).ShouldBeTrue();
        }
    }
}
