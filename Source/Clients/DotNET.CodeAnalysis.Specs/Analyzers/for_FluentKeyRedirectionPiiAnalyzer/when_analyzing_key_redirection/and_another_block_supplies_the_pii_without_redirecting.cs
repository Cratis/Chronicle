// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentKeyRedirectionPiiAnalyzer.when_analyzing_key_redirection;

/// <summary>
/// The precision case. One block redirects and carries nothing personal; another carries the personal value and does not redirect. A rule that only asked whether the read model has [PII] and the projection redirects anywhere would report this correct projection.
/// </summary>
public class and_another_block_supplies_the_pii_without_redirecting : given.a_fluent_key_redirection_pii_analyzer
{
    const string Usage = """
    public record RequestOpened(string RequestId, string Department);

    public record AdvisorNamed([PII] string FullName);

    public record RequestSummary(
        [Key] string Id,
        string Department,
        string AdvisorName);

    public class RequestSummaryProjection : IProjectionFor<RequestSummary>
    {
        public void Define(IProjectionBuilderFor<RequestSummary> builder) => builder
            .From<RequestOpened>(_ => _
                .UsingKey(e => e.RequestId)
                .Set(m => m.Department).To(e => e.Department))
            .From<AdvisorNamed>(_ => _
                .Set(m => m.AdvisorName).To(e => e.FullName));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentKeyRedirectionPiiAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
