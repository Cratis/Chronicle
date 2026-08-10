// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentKeyRedirectionPiiAnalyzer.when_analyzing_key_redirection;

public class and_add_child_carries_a_pii_value : given.a_fluent_key_redirection_pii_analyzer
{
    const string Usage = """
    public record Contact([PII] string EmailAddress);
    public record ContactAdded(string RequestId, Contact Contact);
    public record RequestSummary([Key] string Id, IEnumerable<Contact> Contacts);

    public class RequestSummaryProjection : IProjectionFor<RequestSummary>
    {
        public void Define(IProjectionBuilderFor<RequestSummary> builder) => builder
            .From<ContactAdded>(_ => _
                .AddChild(m => m.Contacts, child => child
                    .{|#0:UsingParentKey|}(e => e.RequestId)
                    .FromObject(e => e.Contact)));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentKeyRedirectionPiiAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.KeyRedirectionPii, DiagnosticSeverity.Warning, "Contacts", "ContactAdded", "Contact", "RequestId", "Id"));

    [Fact] Task should_report_the_child_value_reaching_the_document() => _result;
}
