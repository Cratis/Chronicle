// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_EncryptionKeyCommand;

public class when_combining_flags_incorrectly : Specification
{
    Exception? _verifyAndMigrate;
    Exception? _dryRunAlone;
    Exception? _removeAlone;
    Exception? _dryRunAndRemove;

    void Because()
    {
        _verifyAndMigrate = Catch.Exception(() => EncryptionKeyCommand.Parse(["--verify-encryption-keys", "--migrate-encryption-keys"]));
        _dryRunAlone = Catch.Exception(() => EncryptionKeyCommand.Parse(["--dry-run"]));
        _removeAlone = Catch.Exception(() => EncryptionKeyCommand.Parse(["--verify-encryption-keys", "--remove-migrated-legacy-keys"]));
        _dryRunAndRemove = Catch.Exception(() => EncryptionKeyCommand.Parse(["--migrate-encryption-keys", "--dry-run", "--remove-migrated-legacy-keys"]));
    }

    [Fact] void should_refuse_verify_with_migrate() => _verifyAndMigrate.ShouldBeOfExactType<ArgumentException>();
    [Fact] void should_refuse_a_dry_run_without_migrate() => _dryRunAlone.ShouldBeOfExactType<ArgumentException>();
    [Fact] void should_refuse_removal_without_migrate() => _removeAlone.ShouldBeOfExactType<ArgumentException>();
    [Fact] void should_refuse_a_dry_run_that_removes() => _dryRunAndRemove.ShouldBeOfExactType<ArgumentException>();
}
