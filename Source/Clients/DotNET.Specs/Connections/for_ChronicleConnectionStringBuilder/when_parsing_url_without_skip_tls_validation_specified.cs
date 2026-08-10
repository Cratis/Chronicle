// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder;

public class when_parsing_url_without_skip_tls_validation_specified : Specification
{
    ChronicleConnectionStringBuilder _builder;

    void Establish() => _builder = new ChronicleConnectionStringBuilder("chronicle://localhost:35000");

    [Fact] void should_validate_tls_by_default() => _builder.SkipTlsValidation.ShouldBeFalse();
}
