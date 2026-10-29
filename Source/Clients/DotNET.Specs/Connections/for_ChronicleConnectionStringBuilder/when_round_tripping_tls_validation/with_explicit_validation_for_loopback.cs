// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder.when_round_tripping_tls_validation;

public class with_explicit_validation_for_loopback : Specification
{
    ChronicleConnectionStringBuilder _builder;
    ChronicleConnectionString _result;

    void Establish() => _builder = new("chronicle://localhost:35000?skipTlsValidation=false");

    void Because() => _result = new(_builder.Build());

    [Fact] void should_preserve_validation() => _result.SkipTlsValidation.ShouldBeFalse();
}
