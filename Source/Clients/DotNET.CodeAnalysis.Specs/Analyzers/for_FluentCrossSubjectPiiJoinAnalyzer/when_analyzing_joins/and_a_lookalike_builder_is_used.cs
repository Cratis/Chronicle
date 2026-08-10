// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentCrossSubjectPiiJoinAnalyzer.when_analyzing_joins;

public class and_a_lookalike_builder_is_used : given.a_fluent_cross_subject_pii_join_analyzer
{
    const string Usage = """
    public static class Lookalike
    {
        public interface IJoinBuilder<TReadModel, TEvent>
        {
            IJoinBuilder<TReadModel, TEvent> On<TProperty>(Expression<Func<TReadModel, TProperty>> accessor);
        }

        public interface IProjectionBuilder<TReadModel, TBuilder>
        {
            TBuilder Join<TEvent>(Action<IJoinBuilder<TReadModel, TEvent>> callback);
        }

        public interface IProjectionBuilderFor<TReadModel> : IProjectionBuilder<TReadModel, IProjectionBuilderFor<TReadModel>>
        {
        }
    }

    public record AdvisorNamed([PII] string FullName);
    public record RequestSummary([Key] Guid Id, Guid AdvisorId, string FullName);

    public class RequestSummaryProjection
    {
        public void Define(Lookalike.IProjectionBuilderFor<RequestSummary> builder) => builder
            .Join<AdvisorNamed>(_ => _.On(m => m.AdvisorId));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentCrossSubjectPiiJoinAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_treat_the_lookalike_as_a_chronicle_builder() => _result;
}
