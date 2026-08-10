// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentCrossSubjectPiiJoinAnalyzer.when_analyzing_joins;

public class and_direct_nested_join_uses_the_root_document_subject : given.a_fluent_cross_subject_pii_join_analyzer
{
    const string Usage = """
    public record AdvisorNamed([PII] string FullName);
    public record AdvisorDetails(Guid AdvisorId, string FullName);
    public record RequestSummary([Key] Guid Id, AdvisorDetails? Details);

    public class RequestSummaryProjection : IProjectionFor<RequestSummary>
    {
        public void Define(IProjectionBuilderFor<RequestSummary> builder) => builder
            .Nested(m => m.Details, nested => nested
                .{|#0:Join<AdvisorNamed>|}(_ => _.On(m => m.AdvisorId)));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentCrossSubjectPiiJoinAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.CrossSubjectPiiJoin, DiagnosticSeverity.Error, "FullName", "AdvisorNamed", "AdvisorId"));

    [Fact] Task should_report_the_definite_cross_subject_boundary() => _result;
}
