// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentKeyRedirectionPiiAnalyzer.when_analyzing_key_redirection;

/// <summary>
/// Source syntax cannot prove that historical EventContext subjects, append-time overrides, and the current declared value all equal the redirected key.
/// </summary>
public class and_the_redirected_key_is_the_declared_subject : given.a_fluent_key_redirection_pii_analyzer
{
    const string Usage = """
    public record AdvisorNamed([Subject] string AdvisorId, [PII] string FullName);

    public record AdvisorSummary(
        [Key] string Id,
        string FullName);

    public class AdvisorSummaryProjection : IProjectionFor<AdvisorSummary>
    {
        public void Define(IProjectionBuilderFor<AdvisorSummary> builder) => builder
            .From<AdvisorNamed>(_ => _
                .{|#0:UsingKey|}(e => e.AdvisorId));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentKeyRedirectionPiiAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.KeyRedirectionPii, DiagnosticSeverity.Warning, "FullName", "AdvisorNamed", "AdvisorId", "Id"));

    [Fact] Task should_report_the_key_redirection_pii_diagnostic() => _result;
}
