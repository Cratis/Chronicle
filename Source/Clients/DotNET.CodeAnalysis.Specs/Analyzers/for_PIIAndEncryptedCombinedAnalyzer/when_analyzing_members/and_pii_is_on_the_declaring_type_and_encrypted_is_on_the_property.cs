// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_PIIAndEncryptedCombinedAnalyzer.when_analyzing_members;

public class and_pii_is_on_the_declaring_type_and_encrypted_is_on_the_property : given.a_pii_and_encrypted_combined_analyzer
{
    const string Usage = """
    [PII]
    public class CustomerDetails
    {
        {|#0:[Encrypted]|}
        public string Something { get; init; }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.PIIAndEncryptedCombinedAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.PIIAndEncryptedCombined, DiagnosticSeverity.Error, "Something"));

    [Fact] Task should_report_the_diagnostic() => _result;
}
