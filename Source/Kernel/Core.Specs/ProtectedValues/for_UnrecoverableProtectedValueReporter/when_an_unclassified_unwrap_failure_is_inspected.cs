// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_UnrecoverableProtectedValueReporter;

public class when_an_unclassified_unwrap_failure_is_inspected : Specification
{
    EncryptionKeyUnrecoverable? _classified;

    void Because() => _classified = UnrecoverableProtectedValueReporter.Find(new EncryptionKeyUnwrapFailed(new CryptographicException("unwrap failed")));

    [Fact] void should_not_report_an_unclassified_failure() => _classified.ShouldBeNull();
}
