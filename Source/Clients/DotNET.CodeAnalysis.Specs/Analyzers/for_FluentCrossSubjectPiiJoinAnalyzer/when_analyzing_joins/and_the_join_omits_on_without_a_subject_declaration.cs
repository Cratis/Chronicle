// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentCrossSubjectPiiJoinAnalyzer.when_analyzing_joins;

/// <summary>
/// An omitted On and an absent [Subject] declaration do not prove runtime equality because any append can supply an explicit subject.
/// </summary>
public class and_the_join_omits_on_without_a_subject_declaration : given.a_fluent_cross_subject_pii_join_analyzer
{
    const string Usage = """
    [PII]
    public record DisplayName(string Value);

    public record AdvisorNamed(DisplayName DisplayName);

    public record AdvisorSummary(
        [Key] Guid Id,
        DisplayName DisplayName);

    public class AdvisorSummaryProjection : IProjectionFor<AdvisorSummary>
    {
        public void Define(IProjectionBuilderFor<AdvisorSummary> builder) => builder
            .{|#0:Join<AdvisorNamed>|}(_ => { });
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentCrossSubjectPiiJoinAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.CrossSubjectPiiJoin, DiagnosticSeverity.Error, "DisplayName", "AdvisorNamed", "Id"));

    [Fact] Task should_report_the_cross_subject_pii_join_diagnostic() => _result;
}
