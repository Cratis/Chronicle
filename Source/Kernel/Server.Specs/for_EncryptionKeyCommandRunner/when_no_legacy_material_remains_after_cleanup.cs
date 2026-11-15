// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Server.for_EncryptionKeyCommandRunner;

public class when_no_legacy_material_remains_after_cleanup : given.a_migrator
{
    int? _verificationExit;
    int? _startupExit;

    void Establish() => Returns(new EncryptionKeyStoreUpgradeStatus(true, false, 0, 0, 0, true));

    async Task Because()
    {
        _verificationExit = await Run(true);
        _startupExit = await Start(LegacyKeyHandling.Refuse);
    }

    [Fact] void should_verify_with_exit_zero() => _verificationExit.ShouldEqual(0);
    [Fact] void should_allow_refuse_mode_to_start() => _startupExit.ShouldEqual(null);
}
