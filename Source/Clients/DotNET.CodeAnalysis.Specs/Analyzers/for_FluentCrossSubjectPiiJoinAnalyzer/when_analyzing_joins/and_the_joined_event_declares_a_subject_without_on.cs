// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentCrossSubjectPiiJoinAnalyzer.when_analyzing_joins;

/// <summary>
/// Omitting On selects the local key, but a declared subject makes the joined event's stored runtime ownership unknown.
/// </summary>
public class and_the_joined_event_declares_a_subject_without_on : given.a_fluent_cross_subject_pii_join_analyzer
{
    const string Usage = """
    public record AdvisorNamed([Subject] string AdvisorId, [PII] string FullName);

    public record AdvisorSummary(
        [Key] string Id,
        string FullName);

    public class AdvisorSummaryProjection : IProjectionFor<AdvisorSummary>
    {
        public void Define(IProjectionBuilderFor<AdvisorSummary> builder) => builder
            .{|#0:Join<AdvisorNamed>|}(_ => { });
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentCrossSubjectPiiJoinAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.CrossSubjectPiiJoin, DiagnosticSeverity.Error, "FullName", "AdvisorNamed", "Id"));

    [Fact] Task should_report_the_cross_subject_pii_join_diagnostic() => _result;
}
