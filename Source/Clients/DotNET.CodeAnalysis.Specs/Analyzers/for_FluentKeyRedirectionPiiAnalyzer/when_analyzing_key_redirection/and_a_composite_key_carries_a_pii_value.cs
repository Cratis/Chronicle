// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentKeyRedirectionPiiAnalyzer.when_analyzing_key_redirection;

/// <summary>
/// A composite key canonicalizes to a joined string, which is never any one person's subject.
/// </summary>
public class and_a_composite_key_carries_a_pii_value : given.a_fluent_key_redirection_pii_analyzer
{
    const string Usage = """
    public record AdvisorNamed(string Region, string RequestId, [PII] string FullName);

    public record RequestKey(string Region, string RequestId);

    public record RequestSummary(
        [Key] string Id,
        string AdvisorName);

    public class RequestSummaryProjection : IProjectionFor<RequestSummary>
    {
        public void Define(IProjectionBuilderFor<RequestSummary> builder) => builder
            .From<AdvisorNamed>(_ => _
                .{|#0:UsingCompositeKey<RequestKey>|}(k => k
                    .Set(key => key.Region).To(e => e.Region)
                    .Set(key => key.RequestId).To(e => e.RequestId))
                .Set(m => m.AdvisorName).To(e => e.FullName));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentKeyRedirectionPiiAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.KeyRedirectionPii, DiagnosticSeverity.Warning, "AdvisorName", "AdvisorNamed", "FullName", "a composite key", "Id"));

    [Fact] Task should_report_the_key_redirection_pii_diagnostic() => _result;
}
