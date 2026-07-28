// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentCrossSubjectPiiJoinAnalyzer.when_analyzing_joins;

public class and_the_join_is_on_the_read_models_own_subject : given.a_fluent_cross_subject_pii_join_analyzer
{
    const string Usage = """
    [PII]
    public record DisplayName(string Value);

    public record AdvisorNamed(DisplayName DisplayName);

    public record AdvisorSummary(
        [Key] Guid AdvisorId,
        DisplayName DisplayName);

    public class AdvisorSummaryProjection : IProjectionFor<AdvisorSummary>
    {
        public void Define(IProjectionBuilderFor<AdvisorSummary> builder) => builder
            .Join<AdvisorNamed>(_ => _
                .On(m => m.AdvisorId));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentCrossSubjectPiiJoinAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
