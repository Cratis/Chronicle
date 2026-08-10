// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentCrossSubjectPiiJoinAnalyzer.when_analyzing_joins;

public class and_nested_inside_child_join_uses_the_root_document_subject : given.a_fluent_cross_subject_pii_join_analyzer
{
    const string Usage = """
    public record AdvisorNamed([PII] string FullName);
    public record AdvisorDetails(Guid Id, string FullName);
    public record AdvisorSummary([Key] Guid AdvisorId, AdvisorDetails? Details);
    public record AdvisorBook([Key] Guid Id, IEnumerable<AdvisorSummary> Advisors);

    public class AdvisorBookProjection : IProjectionFor<AdvisorBook>
    {
        public void Define(IProjectionBuilderFor<AdvisorBook> builder) => builder
            .Children(m => m.Advisors, children => children
                .IdentifiedBy(m => m.AdvisorId)
                .Nested(m => m.Details, nested => nested
                    .{|#0:Join<AdvisorNamed>|}(_ => _.On(m => m.Id))));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentCrossSubjectPiiJoinAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.UnprovableCrossSubjectPiiJoin, DiagnosticSeverity.Warning, "FullName", "AdvisorNamed", "Id"));

    [Fact] Task should_report_the_same_apparent_document_subject_as_unprovable() => _result;
}
