// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ServerCertificateValidationWarning.when_deciding;

public class and_the_remote_default_is_used : Specification
{
    ChronicleConnectionString _connectionString;
    bool _result;

    void Establish() => _connectionString = new("chronicle://chronicle.example:35000");

    void Because() => _result = ServerCertificateValidationWarning.AppliesTo(_connectionString.SkipTlsValidation, _connectionString);

    [Fact] void should_not_warn() => _result.ShouldBeFalse();
}
