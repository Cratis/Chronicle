// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Server.for_EncryptionKeyCommandRunner;

public class when_verifying_and_a_shape_is_unresolved : given.a_check
{
    int? _exit;

    void Establish() => Returns(new EncryptionKeyStoreUpgradeStatus(true, false, 0, 0, 1, true));

    async Task Because() => _exit = await Run(true);

    [Fact] void should_ask_for_the_upgrade() => _exit.ShouldEqual(2);
}
