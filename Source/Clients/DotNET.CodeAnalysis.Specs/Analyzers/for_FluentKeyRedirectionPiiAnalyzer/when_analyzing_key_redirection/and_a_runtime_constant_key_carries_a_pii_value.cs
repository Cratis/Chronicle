// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentKeyRedirectionPiiAnalyzer.when_analyzing_key_redirection;

public class and_a_runtime_constant_key_carries_a_pii_value : given.a_fluent_key_redirection_pii_analyzer
{
    const string Usage = """
    public record AdvisorNamed([PII] string FullName);
    public record AdvisorSummary([Key] string Id, string FullName);

    public class AdvisorSummaryProjection : IProjectionFor<AdvisorSummary>
    {
        static string ResolveKey() => DateTime.UtcNow.Year.ToString();

        public void Define(IProjectionBuilderFor<AdvisorSummary> builder) => builder
            .From<AdvisorNamed>(_ => _
                .{|#0:UsingConstantKey|}(ResolveKey()));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentKeyRedirectionPiiAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.KeyRedirectionPii, DiagnosticSeverity.Warning, "FullName", "AdvisorNamed", "a constant key", "Id"));

    [Fact] Task should_report_the_unresolved_constant_key() => _result;
}
