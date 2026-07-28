// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentCrossSubjectPiiJoinAnalyzer.when_analyzing_joins;

public class and_automap_carries_the_pii_value_across : given.a_fluent_cross_subject_pii_join_analyzer
{
    const string Usage = """
    [PII]
    public record DisplayName(string Value);

    public record AdvisorNamed(DisplayName DisplayName);

    public record RequestSummary(
        [Key] Guid Id,
        Guid AdvisorId,
        DisplayName DisplayName);

    public class RequestSummaryProjection : IProjectionFor<RequestSummary>
    {
        public void Define(IProjectionBuilderFor<RequestSummary> builder) => builder
            .{|#0:Join<AdvisorNamed>|}(_ => _
                .On(m => m.AdvisorId));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentCrossSubjectPiiJoinAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.CrossSubjectPiiJoin, DiagnosticSeverity.Error, "DisplayName", "AdvisorNamed", "AdvisorId"));

    [Fact] Task should_report_the_cross_subject_pii_join_diagnostic() => _result;
}
