// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentCrossSubjectPiiJoinAnalyzer.when_analyzing_joins;

public class and_child_inherits_disabled_automap : given.a_fluent_cross_subject_pii_join_analyzer
{
    const string Usage = """
    public record AdvisorNamed([PII] string FullName);
    public record AdvisorSummary([Key] Guid AdvisorId, string FullName);
    public record AdvisorBook([Key] Guid Id, IEnumerable<AdvisorSummary> Advisors);

    public class AdvisorBookProjection : IProjectionFor<AdvisorBook>
    {
        public void Define(IProjectionBuilderFor<AdvisorBook> builder) => builder
            .NoAutoMap()
            .Children(m => m.Advisors, children => children
                .IdentifiedBy(m => m.AdvisorId)
                .Join<AdvisorNamed>());
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentCrossSubjectPiiJoinAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_an_implicitly_mapped_value() => _result;
}
