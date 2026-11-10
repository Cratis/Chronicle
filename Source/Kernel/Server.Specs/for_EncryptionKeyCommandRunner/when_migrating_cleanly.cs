// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_EncryptionKeyCommandRunner;

public class when_migrating_cleanly : given.a_migrator
{
    int? _exit;

    void Establish() => Returns(Report(copied: 3));

    async Task Because() => _exit = await Migrate(remove: true);

    [Fact] void should_exit_clean() => _exit.ShouldEqual(0);
    [Fact] void should_pass_on_the_removal() => _migrator.Received(1).Migrate(false, true, Arg.Any<CancellationToken>());
    [Fact] void should_print_the_counts() => _output.ToString().ShouldContain("keys copied: 3");
    [Fact] void should_not_inspect() => _check.DidNotReceive().Inspect(Arg.Any<bool>(), Arg.Any<CancellationToken>());
}
