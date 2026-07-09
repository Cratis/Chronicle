// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_PiiOnEventSourceIdAnalyzer.when_analyzing_members;

public class and_pii_on_event_source_id_property : given.a_pii_on_event_source_id_analyzer
{
    const string Usage = """
    public record CustomerId(Guid Value) : EventSourceId<Guid>(Value);

    public class Customer
    {
        {|#0:[PII]|}
        public CustomerId Id { get; init; }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.PiiOnEventSourceIdAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.PiiOnEventSourceId, DiagnosticSeverity.Error, "Id", "CustomerId"));

    [Fact] Task should_report_the_diagnostic() => _result;
}
