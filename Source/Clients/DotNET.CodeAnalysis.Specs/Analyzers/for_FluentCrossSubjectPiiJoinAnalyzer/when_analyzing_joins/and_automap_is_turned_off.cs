// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentCrossSubjectPiiJoinAnalyzer.when_analyzing_joins;

/// <summary>
/// With AutoMap turned off nothing crosses implicitly, so an identically named [PII] property on the joined
/// event never reaches the read model and the join is not an error.
/// </summary>
public class and_automap_is_turned_off : given.a_fluent_cross_subject_pii_join_analyzer
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
            .NoAutoMap()
            .Join<AdvisorNamed>(_ => _
                .On(m => m.AdvisorId));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentCrossSubjectPiiJoinAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
