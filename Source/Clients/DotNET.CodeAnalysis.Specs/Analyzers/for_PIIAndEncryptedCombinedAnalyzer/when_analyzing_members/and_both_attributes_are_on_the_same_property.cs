// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_PIIAndEncryptedCombinedAnalyzer.when_analyzing_members;

public class and_both_attributes_are_on_the_same_property : given.a_pii_and_encrypted_combined_analyzer
{
    const string Usage = """
    public record CustomerRegistered(
        {|#0:[PII]|} [Encrypted] string Something);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.PIIAndEncryptedCombinedAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.PIIAndEncryptedCombined, DiagnosticSeverity.Error, "Something"));

    [Fact] Task should_report_the_diagnostic() => _result;
}
