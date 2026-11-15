// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Server.for_EncryptionKeyCommandRunner;

public class when_starting_with_refuse_and_nothing_is_pending : given.a_migrator
{
    int? _exit;

    void Establish() => Returns(new EncryptionKeyStoreUpgradeStatus(true, false, 0, 0, 0, true, 4));

    async Task Because() => _exit = await Start(LegacyKeyHandling.Refuse);

    [Fact] void should_carry_on_starting() => _exit.ShouldEqual(null);
}
