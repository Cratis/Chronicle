// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Server.for_EncryptionKeyCommandRunner;

public class when_verifying_and_the_layout_is_unverified : given.a_check
{
    int? _exit;

    void Establish() => Returns(new EncryptionKeyStoreUpgradeStatus(false, false, 0, 0, 0, false));

    async Task Because() => _exit = await Run(true);

    [Fact] void should_refuse_with_the_layout_code() => _exit.ShouldEqual(4);
}
