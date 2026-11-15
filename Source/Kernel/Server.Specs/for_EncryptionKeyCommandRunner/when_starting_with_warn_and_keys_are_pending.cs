// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Server.for_EncryptionKeyCommandRunner;

public class when_starting_with_warn_and_keys_are_pending : given.a_migrator
{
    int? _exit;

    void Establish() => Returns(new EncryptionKeyStoreUpgradeStatus(true, false, 3, 1, 0, false));

    async Task Because() => _exit = await Start(LegacyKeyHandling.Warn);

    [Fact] void should_carry_on_starting() => _exit.ShouldEqual(null);
    [Fact] void should_only_look_for_earlier_material() => _check.Received(1).Inspect(false, Arg.Any<CancellationToken>());
}
