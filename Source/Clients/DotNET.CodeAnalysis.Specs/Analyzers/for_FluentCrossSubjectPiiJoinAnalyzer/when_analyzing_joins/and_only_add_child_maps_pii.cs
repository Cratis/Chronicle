// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentCrossSubjectPiiJoinAnalyzer.when_analyzing_joins;

public class and_only_add_child_maps_pii : given.a_fluent_cross_subject_pii_join_analyzer
{
    const string Usage = """
    public record Contact([PII] string EmailAddress);
    public record AdvisorNamed(Guid AdvisorId, string PublicName, Contact Contact);
    public record RequestSummary([Key] Guid Id, Guid AdvisorId, string PublicName, IEnumerable<Contact> Contacts);

    public class RequestSummaryProjection : IProjectionFor<RequestSummary>
    {
        public void Define(IProjectionBuilderFor<RequestSummary> builder) => builder
            .Join<AdvisorNamed>(_ => _
                .On(m => m.AdvisorId)
                .AddChild(m => m.Contacts, e => e.Contact));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentCrossSubjectPiiJoinAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_treat_the_separate_child_operation_as_join_content() => _result;
}
