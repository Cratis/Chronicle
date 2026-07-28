// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentCrossSubjectPiiJoinAnalyzer.when_analyzing_joins;

/// <summary>
/// The joined event carries PII, but under a name the read model does not have and does not map, so AutoMap
/// never carries it across and nothing of another subject's is materialized.
/// </summary>
public class and_no_pii_reaches_the_read_model : given.a_fluent_cross_subject_pii_join_analyzer
{
    const string Usage = """
    [PII]
    public record DisplayName(string Value);

    public record AdvisorNamed(DisplayName DisplayName, string Department);

    public record RequestSummary(
        [Key] Guid Id,
        Guid AdvisorId,
        string Department);

    public class RequestSummaryProjection : IProjectionFor<RequestSummary>
    {
        public void Define(IProjectionBuilderFor<RequestSummary> builder) => builder
            .Join<AdvisorNamed>(_ => _
                .On(m => m.AdvisorId)
                .Set(m => m.Department).To(e => e.Department));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentCrossSubjectPiiJoinAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
