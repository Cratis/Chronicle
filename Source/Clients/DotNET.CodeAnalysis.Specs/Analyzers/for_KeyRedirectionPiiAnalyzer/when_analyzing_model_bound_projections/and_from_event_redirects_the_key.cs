// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_KeyRedirectionPiiAnalyzer.when_analyzing_model_bound_projections;

/// <summary>
/// The model-bound spelling of UsingKey: the key argument names an event property, so the document is subjected to that value rather than to the stream the event was appended to.
/// </summary>
public class and_from_event_redirects_the_key : given.a_key_redirection_pii_analyzer
{
    const string Usage = """
    public record AdvisorNamed(string RequestId, [PII] string FullName);

    {|#0:[FromEvent<AdvisorNamed>(key: "RequestId")]|}
    public record RequestSummary(
        [Key] string Id,
        string FullName);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.KeyRedirectionPiiAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.KeyRedirectionPii, DiagnosticSeverity.Warning, "FullName", "AdvisorNamed", "FullName", "RequestId", "Id"));

    [Fact] Task should_report_the_key_redirection_pii_diagnostic() => _result;
}
