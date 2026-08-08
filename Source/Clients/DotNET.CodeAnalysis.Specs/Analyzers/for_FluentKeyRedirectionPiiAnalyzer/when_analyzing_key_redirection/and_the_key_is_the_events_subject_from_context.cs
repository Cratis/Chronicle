// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentKeyRedirectionPiiAnalyzer.when_analyzing_key_redirection;

/// <summary>
/// Keying by the event's compliance subject puts the document under the value's owner, which is the correct shape.
/// </summary>
public class and_the_key_is_the_events_subject_from_context : given.a_fluent_key_redirection_pii_analyzer
{
    const string Usage = """
    public record AdvisorNamed([PII] string FullName);

    public record AdvisorSummary(
        [Key] string Id,
        string AdvisorName);

    public class AdvisorSummaryProjection : IProjectionFor<AdvisorSummary>
    {
        public void Define(IProjectionBuilderFor<AdvisorSummary> builder) => builder
            .From<AdvisorNamed>(_ => _
                .UsingKeyFromContext(c => c.Subject)
                .Set(m => m.AdvisorName).To(e => e.FullName));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentKeyRedirectionPiiAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
