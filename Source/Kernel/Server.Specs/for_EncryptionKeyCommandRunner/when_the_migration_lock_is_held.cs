// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_EncryptionKeyCommandRunner;

public class when_the_migration_lock_is_held : given.a_migrator
{
    int? _exit;

    void Establish() => Returns(Report(complete: false) with { MigrationLocked = true });

    async Task Because() => _exit = await Migrate();

    [Fact] void should_return_a_distinct_exit_code() => _exit.ShouldEqual(6);
    [Fact] void should_explain_operator_recovery() => _output.ToString().ShouldContain("stop all migrators");
}
