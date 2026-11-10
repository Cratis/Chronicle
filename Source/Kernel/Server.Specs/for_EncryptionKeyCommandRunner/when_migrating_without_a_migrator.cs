// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Server.for_EncryptionKeyCommandRunner;

public class when_migrating_without_a_migrator : given.a_check
{
    int? _exit;

    async Task Because() => _exit = await EncryptionKeyCommandRunner.Run(null, NullLogger.Instance, new EncryptionKeyCommand(false, [], true), _output);

    [Fact] void should_exit_clean() => _exit.ShouldEqual(0);
    [Fact] void should_say_there_is_nothing_to_migrate() => _output.ToString().ShouldContain("no keys stored under earlier names to migrate");
}
