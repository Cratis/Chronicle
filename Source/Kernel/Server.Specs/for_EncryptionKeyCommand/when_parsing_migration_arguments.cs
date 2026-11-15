// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_EncryptionKeyCommand;

public class when_parsing_migration_arguments : Specification
{
    EncryptionKeyCommand _migrate;
    EncryptionKeyCommand _dryRun;
    EncryptionKeyCommand _remove;

    void Because()
    {
        _migrate = EncryptionKeyCommand.Parse(["--urls", "http://x", "--migrate-encryption-keys"]);
        _dryRun = EncryptionKeyCommand.Parse(["--migrate-encryption-keys", "--dry-run", "--urls", "http://x"]);
        _remove = EncryptionKeyCommand.Parse(["--migrate-encryption-keys", "--remove-migrated-legacy-keys"]);
    }

    [Fact] void should_recognize_the_migrate_flag() => _migrate.Migrate.ShouldBeTrue();
    [Fact] void should_not_dry_run_without_the_flag() => _migrate.DryRun.ShouldBeFalse();
    [Fact] void should_not_remove_without_the_flag() => _migrate.RemoveMigratedLegacy.ShouldBeFalse();
    [Fact] void should_exit_afterwards() => _migrate.ExitsAfterwards.ShouldBeTrue();
    [Fact] void should_leave_the_other_arguments_for_the_host() => _dryRun.RemainingArgs.ShouldEqual(["--urls", "http://x"]);
    [Fact] void should_recognize_the_dry_run_flag() => _dryRun.DryRun.ShouldBeTrue();
    [Fact] void should_recognize_the_remove_flag() => _remove.RemoveMigratedLegacy.ShouldBeTrue();
}
