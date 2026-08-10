// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentKeyRedirectionPiiAnalyzer.when_analyzing_key_redirection;

public class and_a_lookalike_builder_is_used : given.a_fluent_key_redirection_pii_analyzer
{
    const string Usage = """
    public static class Lookalike
    {
        public interface IFromBuilder<TReadModel, TEvent>
        {
            IFromBuilder<TReadModel, TEvent> UsingKey<TProperty>(Expression<Func<TEvent, TProperty>> accessor);
        }

        public interface IProjectionBuilder<TReadModel, TBuilder>
        {
            TBuilder From<TEvent>(Action<IFromBuilder<TReadModel, TEvent>> callback);
        }

        public interface IProjectionBuilderFor<TReadModel> : IProjectionBuilder<TReadModel, IProjectionBuilderFor<TReadModel>>
        {
        }
    }

    public record AdvisorNamed(string RequestId, [PII] string FullName);
    public record RequestSummary([Key] string Id, string FullName);

    public class RequestSummaryProjection
    {
        public void Define(Lookalike.IProjectionBuilderFor<RequestSummary> builder) => builder
            .From<AdvisorNamed>(_ => _.UsingKey(e => e.RequestId));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentKeyRedirectionPiiAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_treat_the_lookalike_as_a_chronicle_builder() => _result;
}
