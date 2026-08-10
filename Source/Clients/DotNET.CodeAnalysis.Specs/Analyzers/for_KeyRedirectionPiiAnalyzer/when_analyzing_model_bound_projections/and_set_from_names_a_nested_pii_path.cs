// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_KeyRedirectionPiiAnalyzer.when_analyzing_model_bound_projections;

public class and_set_from_names_a_nested_pii_path : given.a_key_redirection_pii_analyzer
{
    const string Usage = """
    public record Advisor([PII] string FullName);
    public record AdvisorAssigned(string RequestId, Advisor Advisor);

    [FromEvent<AdvisorAssigned>(key: "RequestId")]
    public record RequestSummary(
        [Key] string Id,
        [SetFrom<AdvisorAssigned>("Advisor.FullName")] string AdvisorName);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.KeyRedirectionPiiAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.KeyRedirectionPii, DiagnosticSeverity.Warning, "AdvisorName", "AdvisorAssigned", "Advisor.FullName", "RequestId", "Id"));

    [Fact] Task should_report_the_nested_event_property_path() => _result;
}
