// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.for_ServerCertificateValidationWarning.when_deciding;

public class and_validation_is_skipped_for_ipv4_loopback : Specification
{
    bool _result;

    void Because() => _result = ServerCertificateValidationWarning.AppliesTo(true, new ChronicleConnectionString("chronicle://127.0.0.1:35000,127.42.3.4:35000"));

    [Fact] void should_not_warn() => _result.ShouldBeFalse();
}
