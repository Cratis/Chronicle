// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentKeyRedirectionPiiAnalyzer.when_analyzing_key_redirection;

public class and_an_opaque_qualified_static_method_result_cannot_suppress_automap : given.a_fluent_key_redirection_pii_analyzer
{
    const string Usage = """
    public record AdvisorNamed(string RequestId, [PII] string FullName);

    [NoAutoMap]
    public record RequestSummary([Key] string Id, string FullName);

    public static class Helpers
    {
        public static IProjectionBuilderFor<RequestSummary> Enable(IProjectionBuilderFor<RequestSummary> builder)
        {
            var result = builder;
            return result;
        }
    }

    public class RequestSummaryProjection : IProjectionFor<RequestSummary>
    {
        public void Define(IProjectionBuilderFor<RequestSummary> builder) => Helpers.Enable(builder)
            .From<AdvisorNamed>(from => from.{|#0:UsingKey|}(e => e.RequestId));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentKeyRedirectionPiiAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.KeyRedirectionPii, DiagnosticSeverity.Warning, "FullName", "AdvisorNamed", "RequestId", "Id"));

    [Fact] Task should_complete_and_report_the_key_redirection() => _result;
}
