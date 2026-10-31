// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.for_ServerCertificateValidationWarning.when_deciding;

public class and_validation_is_enforced : Specification
{
    bool _result;

    void Because() => _result = ServerCertificateValidationWarning.AppliesTo(false, new ChronicleConnectionString("chronicle+srv://server.example"));

    [Fact] void should_not_warn() => _result.ShouldBeFalse();
}
