// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentKeyRedirectionPiiAnalyzer.when_analyzing_key_redirection;

public class and_a_collection_element_contains_pii : given.a_fluent_key_redirection_pii_analyzer
{
    const string Usage = """
    public record Contact([PII] string Email);

    public record AdvisorContacts(string RequestId, IReadOnlyList<Contact> Contacts);

    public record RequestSummary(
        [Key] string Id,
        IReadOnlyList<Contact> Contacts);

    public class RequestSummaryProjection : IProjectionFor<RequestSummary>
    {
        public void Define(IProjectionBuilderFor<RequestSummary> builder) => builder
            .From<AdvisorContacts>(_ => _
                .{|#0:UsingKey|}(e => e.RequestId));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentKeyRedirectionPiiAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.KeyRedirectionPii, DiagnosticSeverity.Warning, "Contacts", "AdvisorContacts", "RequestId", "Id"));

    [Fact] Task should_report_the_key_redirection_pii_diagnostic() => _result;
}
