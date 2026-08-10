// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentKeyRedirectionPiiAnalyzer.when_analyzing_key_redirection;

public class and_child_using_key_only_identifies_the_child : given.a_fluent_key_redirection_pii_analyzer
{
    const string Usage = """
    public record ContactAdded(string ContactId, [PII] string EmailAddress);
    public record Contact([Key] string ContactId, string EmailAddress);
    public record RequestSummary([Key] string Id, IEnumerable<Contact> Contacts);

    public class RequestSummaryProjection : IProjectionFor<RequestSummary>
    {
        public void Define(IProjectionBuilderFor<RequestSummary> builder) => builder
            .Children(m => m.Contacts, children => children
                .IdentifiedBy(m => m.ContactId)
                .From<ContactAdded>(_ => _
                    .UsingKey(e => e.ContactId)
                    .Set(m => m.EmailAddress).To(e => e.EmailAddress)));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentKeyRedirectionPiiAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_document_key_redirection() => _result;
}
