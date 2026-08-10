// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentKeyRedirectionPiiAnalyzer.when_analyzing_key_redirection;

/// <summary>
/// Keying off an EventContext member other than the event's own subject redirects the document exactly as an event property does.
/// </summary>
public class and_using_key_from_context_carries_a_pii_value : given.a_fluent_key_redirection_pii_analyzer
{
    const string Usage = """
    public record AdvisorNamed([PII] string FullName);

    public record RequestSummary(
        [Key] string Id,
        string AdvisorName);

    public class RequestSummaryProjection : IProjectionFor<RequestSummary>
    {
        public void Define(IProjectionBuilderFor<RequestSummary> builder) => builder
            .From<AdvisorNamed>(_ => _
                .{|#0:UsingKeyFromContext|}(c => c.CorrelationId)
                .Set(m => m.AdvisorName).To(e => e.FullName));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentKeyRedirectionPiiAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.KeyRedirectionPii, DiagnosticSeverity.Warning, "AdvisorName", "AdvisorNamed", "FullName", "EventContext.CorrelationId", "Id"));

    [Fact] Task should_report_the_key_redirection_pii_diagnostic() => _result;
}
