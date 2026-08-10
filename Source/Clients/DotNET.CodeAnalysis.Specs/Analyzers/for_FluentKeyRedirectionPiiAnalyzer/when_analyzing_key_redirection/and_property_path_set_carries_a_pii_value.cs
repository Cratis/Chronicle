// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentKeyRedirectionPiiAnalyzer.when_analyzing_key_redirection;

public class and_property_path_set_carries_a_pii_value : given.a_fluent_key_redirection_pii_analyzer
{
    const string Usage = """
    public record AdvisorProfile([PII] string FullName);
    public record AdvisorProfileAssigned(string RequestId, AdvisorProfile Profile);
    public record RequestSummary([Key] string Id, AdvisorProfile Advisor);

    public class RequestSummaryProjection : IProjectionFor<RequestSummary>
    {
        public void Define(IProjectionBuilderFor<RequestSummary> builder) => builder
            .From<AdvisorProfileAssigned>(_ => _
                .{|#0:UsingKey|}(e => e.RequestId)
                .Set("Advisor").To("Profile"));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentKeyRedirectionPiiAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.KeyRedirectionPii, DiagnosticSeverity.Warning, "Advisor", "AdvisorProfileAssigned", "Profile", "RequestId", "Id"));

    [Fact] Task should_report_the_property_path_mapping() => _result;
}
