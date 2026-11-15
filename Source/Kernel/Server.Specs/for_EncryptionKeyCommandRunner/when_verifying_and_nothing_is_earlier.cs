// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Server.for_EncryptionKeyCommandRunner;

public class when_verifying_and_nothing_is_earlier : given.a_check
{
    int? _exit;

    void Establish() => Returns(new EncryptionKeyStoreUpgradeStatus(true, false, 0, 0, 0, true));

    async Task Because() => _exit = await Run(true);

    [Fact] void should_exit_cleanly() => _exit.ShouldEqual(0);
}
