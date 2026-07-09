// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReservedSubjectPropertyAnalyzer.when_analyzing_read_model;

public class and_read_model_has_reserved_subject_property : given.a_reserved_subject_property_analyzer
{
    const string Usage = """
    [ReadModel]
    public class Customer
    {
        public Guid Id { get; init; }

        public string {|#0:_subject|} { get; init; }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReservedSubjectPropertyAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.ReservedSubjectProperty, DiagnosticSeverity.Error, "Customer"));

    [Fact] Task should_report_the_diagnostic() => _result;
}
