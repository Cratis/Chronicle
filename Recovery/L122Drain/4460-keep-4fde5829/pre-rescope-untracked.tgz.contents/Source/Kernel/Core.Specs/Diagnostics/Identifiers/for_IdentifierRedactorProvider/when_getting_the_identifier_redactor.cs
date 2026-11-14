// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Diagnostics.Identifiers.for_IdentifierRedactorProvider;

public class when_getting_the_identifier_redactor : Specification
{
    Redactor _result;

    void Because() => _result = new IdentifierRedactorProvider(Options.Create(new ChronicleOptions())).GetRedactor(ChronicleDataClassifications.EventSourceIdentifier);

    [Fact] void should_return_an_identifier_redactor() => _result.ShouldBeOfExactType<IdentifierRedactor>();
}
