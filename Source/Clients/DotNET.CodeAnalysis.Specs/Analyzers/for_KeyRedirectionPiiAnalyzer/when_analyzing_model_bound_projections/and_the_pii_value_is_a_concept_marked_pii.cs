// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_KeyRedirectionPiiAnalyzer.when_analyzing_model_bound_projections;

/// <summary>
/// The [PII] marking may sit on the value's own type rather than on the event member, as it does for a ConceptAs value object.
/// </summary>
public class and_the_pii_value_is_a_concept_marked_pii : given.a_key_redirection_pii_analyzer
{
    const string Usage = """
    [PII]
    public record DisplayName(string Value);

    public record AdvisorNamed(string RequestId, DisplayName DisplayName);

    {|#0:[FromEvent<AdvisorNamed>(key: "RequestId")]|}
    public record RequestSummary(
        [Key] string Id,
        DisplayName DisplayName);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.KeyRedirectionPiiAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.KeyRedirectionPii, DiagnosticSeverity.Warning, "DisplayName", "AdvisorNamed", "DisplayName", "RequestId", "Id"));

    [Fact] Task should_report_the_key_redirection_pii_diagnostic() => _result;
}
