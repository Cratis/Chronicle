// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentKeyRedirectionPiiAnalyzer.when_analyzing_key_redirection;

public class and_automap_is_enabled_before_a_forwarded_builder : given.a_fluent_key_redirection_pii_analyzer
{
    const string Usage = """
    public record AdvisorNamed(string RequestId, [PII] string FullName);

    [NoAutoMap]
    public record RequestSummary([Key] string Id, string FullName);

    public class RequestSummaryProjection : IProjectionFor<RequestSummary>
    {
        public void Define(IProjectionBuilderFor<RequestSummary> builder)
        {
            builder.AutoMap();
            Forward(builder).From<AdvisorNamed>(from => from.{|#0:UsingKey|}(e => e.RequestId));
        }

        static IProjectionBuilderFor<RequestSummary> Forward(IProjectionBuilderFor<RequestSummary> builder) => builder;
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentKeyRedirectionPiiAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.KeyRedirectionPii, DiagnosticSeverity.Warning, "FullName", "AdvisorNamed", "RequestId", "Id"));

    [Fact] Task should_complete_and_report_the_key_redirection() => _result;
}
