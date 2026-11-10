// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_EncryptionKeyCommandRunner;

public class when_migrating_and_it_fails : given.a_migrator
{
    int? _failed;
    int? _incomplete;
    int? _thrown;

    async Task Because()
    {
        Returns(Report(copied: 2, failed: 1));
        _failed = await Migrate();
        Returns(Report(complete: false));
        _incomplete = await Migrate();

        var throwing = Substitute.For<Cratis.Chronicle.Storage.Compliance.IEncryptionKeyStoreMigrator>();
        throwing.Migrate(Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns<Task<Cratis.Chronicle.Storage.Compliance.EncryptionKeyMigrationReport>>(_ => throw new InvalidOperationException("secret/store/default/customer-42/1"));
        _thrown = await Migrate(migrator: throwing);
    }

    [Fact] void should_fail_when_a_copy_failed() => _failed.ShouldEqual(5);
    [Fact] void should_fail_when_it_did_not_finish() => _incomplete.ShouldEqual(5);
    [Fact] void should_fail_when_it_throws() => _thrown.ShouldEqual(5);
    [Fact] void should_not_print_what_the_failure_said() => _output.ToString().Contains("customer-42", StringComparison.Ordinal).ShouldBeFalse();
}
