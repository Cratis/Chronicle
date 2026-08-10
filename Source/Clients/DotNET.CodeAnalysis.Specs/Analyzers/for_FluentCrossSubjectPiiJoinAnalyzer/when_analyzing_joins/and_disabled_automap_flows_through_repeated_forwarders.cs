// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentCrossSubjectPiiJoinAnalyzer.when_analyzing_joins;

public class and_disabled_automap_flows_through_repeated_forwarders : given.a_fluent_cross_subject_pii_join_analyzer
{
    const string Usage = """
    public record AdvisorNamed([PII] string FullName);
    public record RequestSummary([Key] Guid Id, Guid AdvisorId, string FullName);

    public class RequestSummaryProjection : IProjectionFor<RequestSummary>
    {
        public void Define(IProjectionBuilderFor<RequestSummary> builder)
        {
            builder.NoAutoMap();
            Forward(Forward(builder)).Join<AdvisorNamed>(_ => _.On(m => m.AdvisorId));
        }

        static IProjectionBuilderFor<RequestSummary> Forward(IProjectionBuilderFor<RequestSummary> builder) => builder;
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentCrossSubjectPiiJoinAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_an_implicitly_mapped_value() => _result;
}
