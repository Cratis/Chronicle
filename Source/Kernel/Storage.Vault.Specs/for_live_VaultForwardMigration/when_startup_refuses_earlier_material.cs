// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Storage.Vault.for_live_VaultForwardMigration;

[Collection(LiveVaultCollection.Name)]
public class when_startup_refuses_earlier_material(LiveVaultFixture fixture) : given.a_live_migration(fixture)
{
    int? _before;
    int? _warn;
    int? _verification;
    int? _after;

    async Task Establish() => await Put(Legacy);

    async Task Because()
    {
        _before = await ServerCommand.Run(_check, _migrator, LegacyKeyHandling.Refuse);
        _warn = await ServerCommand.Run(_check, _migrator, LegacyKeyHandling.Warn);
        await Migrate();
        _verification = await ServerCommand.Run(_check, _migrator, LegacyKeyHandling.Refuse, "--verify-encryption-keys");
        _after = await ServerCommand.Run(_check, _migrator, LegacyKeyHandling.Refuse);
    }

    [Fact] void should_refuse_startup_before_verified_migration() => _before.ShouldEqual(2);
    [Fact] void should_keep_warn_startup_behavior() => (_warn is null).ShouldBeTrue();
    [Fact] void should_verify_the_real_vault_store() => _verification.ShouldEqual(0);
    [Fact] void should_continue_startup_after_verification() => (_after is null).ShouldBeTrue();
}
