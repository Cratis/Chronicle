// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_EncryptionKeyCommandRunner;

public class when_dry_running_a_migration : given.a_migrator
{
    int? _wouldCopy;
    int? _nothing;

    async Task Because()
    {
        Returns(Report(dryRun: true, copied: 2));
        _wouldCopy = await Migrate(dryRun: true);
        Returns(Report(dryRun: true));
        _nothing = await Migrate(dryRun: true);
    }

    [Fact] void should_ask_for_the_migration_when_it_would_copy() => _wouldCopy.ShouldEqual(2);
    [Fact] void should_exit_clean_when_there_is_nothing_to_copy() => _nothing.ShouldEqual(0);
    [Fact] void should_pass_on_the_dry_run() => _migrator.Received(2).Migrate(true, false, Arg.Any<CancellationToken>());
    [Fact] void should_say_what_it_would_copy() => _output.ToString().ShouldContain("keys that would be copied: 2");
}
