// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_EncryptionKeyCommandRunner;

public class when_verifying_and_the_check_fails : given.a_check
{
    int? _exit;

    void Establish() => _check.Inspect(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns<Task<Cratis.Chronicle.Storage.Compliance.EncryptionKeyStoreUpgradeStatus>>(_ => throw new InvalidOperationException());

    async Task Because() => _exit = await Run(true);

    [Fact] void should_exit_with_the_layout_unusable_code() => _exit.ShouldEqual(4);
}
