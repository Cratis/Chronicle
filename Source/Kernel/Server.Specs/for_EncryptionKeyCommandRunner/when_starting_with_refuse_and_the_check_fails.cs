// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Server.for_EncryptionKeyCommandRunner;

public class when_starting_with_refuse_and_the_check_fails : given.a_migrator
{
    int? _exit;

    void Establish() => _check.Inspect(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns<Task<Cratis.Chronicle.Storage.Compliance.EncryptionKeyStoreUpgradeStatus>>(_ => throw new HttpRequestException("unreachable"));

    async Task Because() => _exit = await Start(LegacyKeyHandling.Refuse);

    [Fact] void should_not_start() => _exit.ShouldEqual(4);
}
