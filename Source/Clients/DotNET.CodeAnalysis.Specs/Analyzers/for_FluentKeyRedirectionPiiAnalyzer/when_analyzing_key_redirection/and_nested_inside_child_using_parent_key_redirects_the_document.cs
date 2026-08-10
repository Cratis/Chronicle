// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentKeyRedirectionPiiAnalyzer.when_analyzing_key_redirection;

public class and_nested_inside_child_using_parent_key_redirects_the_document : given.a_fluent_key_redirection_pii_analyzer
{
    const string Usage = """
    public record ContactDetailsChanged(string RequestId, string ContactId, [PII] string EmailAddress);
    public record ContactDetails([Key] string ContactId, string EmailAddress);
    public record Contact([Key] string ContactId, ContactDetails? Details);
    public record RequestSummary([Key] string Id, IEnumerable<Contact> Contacts);

    public class RequestSummaryProjection : IProjectionFor<RequestSummary>
    {
        public void Define(IProjectionBuilderFor<RequestSummary> builder) => builder
            .Children(m => m.Contacts, children => children
                .IdentifiedBy(m => m.ContactId)
                .Nested(m => m.Details, nested => nested
                    .From<ContactDetailsChanged>(_ => _
                        .{|#0:UsingParentKey|}(e => e.RequestId)
                        .Set(m => m.EmailAddress).To(e => e.EmailAddress))));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentKeyRedirectionPiiAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.KeyRedirectionPii, DiagnosticSeverity.Warning, "EmailAddress", "ContactDetailsChanged", "EmailAddress", "RequestId", "Id"));

    [Fact] Task should_report_the_containing_document_redirection() => _result;
}
