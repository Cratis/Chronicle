// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentKeyRedirectionPiiAnalyzer.when_analyzing_key_redirection;

public class and_non_pii_add_child_redirects_while_root_pii_stays_on_default_key : given.a_fluent_key_redirection_pii_analyzer
{
    const string Usage = """
    public record ContactAdded(string RequestId, string ContactId, string Label, [PII] string FullName);
    public record Contact([Key] string ContactId, string Label);
    public record RequestSummary([Key] string Id, string FullName, IEnumerable<Contact> Contacts);

    public class RequestSummaryProjection : IProjectionFor<RequestSummary>
    {
        public void Define(IProjectionBuilderFor<RequestSummary> builder) => builder
            .From<ContactAdded>(_ => _
                .AddChild(m => m.Contacts, child => child
                    .UsingParentKey(e => e.RequestId)
                    .UsingKey(e => e.ContactId)));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentKeyRedirectionPiiAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_pair_the_root_pii_with_the_child_redirection() => _result;
}
