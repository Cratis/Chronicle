// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Server.for_EncryptionKeyCommandRunner;

public class when_verifying_and_keys_are_earlier : given.a_check
{
    int? _exit;

    void Establish() => Returns(new EncryptionKeyStoreUpgradeStatus(true, false, 3, 1, 0, true));

    async Task Because() => _exit = await Run(true);

    [Fact] void should_ask_for_the_upgrade() => _exit.ShouldEqual(2);
    [Fact] void should_print_the_counts() => _output.ToString().ShouldContain("Earlier keys: 3");
}
