// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentCrossSubjectPiiJoinAnalyzer.when_analyzing_joins;

/// <summary>
/// A child Join can omit On, and an event declaration still cannot prove the persisted subject of every event.
/// </summary>
public class and_the_joined_event_declares_a_subject_without_on : given.a_fluent_cross_subject_pii_join_analyzer
{
    const string Usage = """
    public record AdvisorNamed([Subject] string AdvisorId, [PII] string FullName);

    public record AdvisorSummary(
        [Key] string AdvisorId,
        string FullName);

    public record AdvisorBook([Key] string Id, IEnumerable<AdvisorSummary> Advisors);

    public class AdvisorBookProjection : IProjectionFor<AdvisorBook>
    {
        public void Define(IProjectionBuilderFor<AdvisorBook> builder) => builder
            .Children(m => m.Advisors, children => children
                .IdentifiedBy(m => m.AdvisorId)
                .{|#0:Join<AdvisorNamed>|}());
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentCrossSubjectPiiJoinAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.UnprovableCrossSubjectPiiJoin, DiagnosticSeverity.Warning, "FullName", "AdvisorNamed", "Id"));

    [Fact] Task should_report_the_unprovable_subject_warning() => _result;
}
