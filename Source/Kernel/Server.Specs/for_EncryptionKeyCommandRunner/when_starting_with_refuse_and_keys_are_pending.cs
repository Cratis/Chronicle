// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Server.for_EncryptionKeyCommandRunner;

public class when_starting_with_refuse_and_keys_are_pending : given.a_migrator
{
    int? _exit;

    void Establish() => Returns(new EncryptionKeyStoreUpgradeStatus(true, false, 3, 1, 0, true));

    async Task Because() => _exit = await Start(LegacyKeyHandling.Refuse);

    [Fact] void should_not_start() => _exit.ShouldEqual(2);
    [Fact] void should_inspect_the_whole_key_store() => _check.Received(1).Inspect(true, Arg.Any<CancellationToken>());
    [Fact] void should_not_migrate() => _migrator.DidNotReceive().Migrate(Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
}
