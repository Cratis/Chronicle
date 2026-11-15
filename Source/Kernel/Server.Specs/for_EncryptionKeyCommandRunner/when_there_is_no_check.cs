// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Server.for_EncryptionKeyCommandRunner;

public class when_there_is_no_check : Specification
{
    int? _verifying;
    int? _starting;

    async Task Because()
    {
        _verifying = await EncryptionKeyCommandRunner.Run(null, NullLogger.Instance, new EncryptionKeyCommand(true, []), new StringWriter());
        _starting = await EncryptionKeyCommandRunner.Run(null, NullLogger.Instance, new EncryptionKeyCommand(false, []), new StringWriter());
    }

    [Fact] void should_exit_cleanly_when_verifying() => _verifying.ShouldEqual(0);
    [Fact] void should_carry_on_starting() => _starting.ShouldBeNull();
}
