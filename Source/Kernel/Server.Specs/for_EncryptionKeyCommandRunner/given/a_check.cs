// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Server.for_EncryptionKeyCommandRunner.given;

public class a_check : Specification
{
    protected IEncryptionKeyStoreUpgradeCheck _check = default!;
    protected StringWriter _output = default!;

    void Establish()
    {
        _check = Substitute.For<IEncryptionKeyStoreUpgradeCheck>();
        _output = new StringWriter();
    }

    protected void Returns(EncryptionKeyStoreUpgradeStatus status) =>
        _check.Inspect(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(status);

    protected Task<int?> Run(bool verify, IEncryptionKeyStoreUpgradeCheck? check = null) =>
        EncryptionKeyCommandRunner.Run(check ?? _check, NullLogger.Instance, new EncryptionKeyCommand(verify, []), _output);
}
