// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder.when_round_tripping_tls_validation;

public class with_the_remote_default : Specification
{
    ChronicleConnectionStringBuilder _builder;
    ChronicleConnectionString _result;

    void Establish() => _builder = new("chronicle://chronicle.example:35000");

    void Because() => _result = new(_builder.Build());

    [Fact] void should_validate_the_certificate() => _result.SkipTlsValidation.ShouldBeFalse();
}
