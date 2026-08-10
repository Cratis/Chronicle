// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentKeyRedirectionPiiAnalyzer.when_analyzing_key_redirection;

public class and_nested_property_paths_carry_a_pii_value : given.a_fluent_key_redirection_pii_analyzer
{
    const string Usage = """
    public record RequestReference(string Id);
    public record EventAdvisor([PII] string Name);
    public record AdvisorAssigned(RequestReference Request, EventAdvisor Advisor);
    public record ReadModelAdvisor(string Name);
    public record RequestSummary([Key] string Id, ReadModelAdvisor Advisor);

    public class RequestSummaryProjection : IProjectionFor<RequestSummary>
    {
        public void Define(IProjectionBuilderFor<RequestSummary> builder) => builder
            .From<AdvisorAssigned>(_ => _
                .{|#0:UsingKey|}(e => e.Request.Id)
                .Set(m => m.Advisor.Name).To(e => e.Advisor.Name));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentKeyRedirectionPiiAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.KeyRedirectionPii, DiagnosticSeverity.Warning, "Advisor.Name", "AdvisorAssigned", "Request.Id", "Id"));

    [Fact] Task should_report_the_nested_pii_source_and_target_paths() => _result;
}
