// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentCrossSubjectPiiJoinAnalyzer.when_analyzing_joins;

/// <summary>
/// [NoAutoMap] on the read model turns AutoMap off for every one of its properties, so an identically named
/// [PII] property on the joined event never reaches it.
/// </summary>
public class and_the_read_model_disables_automap : given.a_fluent_cross_subject_pii_join_analyzer
{
    const string Usage = """
    [PII]
    public record DisplayName(string Value);

    public record AdvisorNamed(DisplayName DisplayName);

    [NoAutoMap]
    public record RequestSummary(
        [Key] Guid Id,
        Guid AdvisorId,
        DisplayName DisplayName);

    public class RequestSummaryProjection : IProjectionFor<RequestSummary>
    {
        public void Define(IProjectionBuilderFor<RequestSummary> builder) => builder
            .Join<AdvisorNamed>(_ => _
                .On(m => m.AdvisorId));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentCrossSubjectPiiJoinAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
