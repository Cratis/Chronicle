// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Server.for_EncryptionKeyCommandRunner;

public class when_starting_and_keys_are_earlier : given.a_check
{
    int? _exit;

    void Establish() => Returns(new EncryptionKeyStoreUpgradeStatus(true, false, 3, 1, 0, false));

    async Task Because() => _exit = await Run(false);

    [Fact] void should_carry_on_starting() => _exit.ShouldEqual(null);
}
