// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.for_ServerCertificateValidationWarning.when_deciding;

public class and_tls_is_disabled : Specification
{
    bool _result;

    void Because() => _result = ServerCertificateValidationWarning.AppliesTo(true, new ChronicleConnectionString("chronicle://server.example:35000/?disableTls=true"));

    [Fact] void should_not_warn() => _result.ShouldBeFalse();
}
