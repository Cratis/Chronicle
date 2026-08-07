// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentKeyRedirectionPiiAnalyzer.when_analyzing_key_redirection;

/// <summary>
/// The context-sourced parent key redirects the parent document the same way.
/// </summary>
public class and_using_parent_key_from_context_carries_a_pii_value : given.a_fluent_key_redirection_pii_analyzer
{
    const string Usage = """
    public record ContactAdded(string ContactId, [PII] string EmailAddress);

    public record Contact(
        [Key] string ContactId,
        string EmailAddress);

    public record RequestSummary(
        [Key] string Id,
        IEnumerable<Contact> Contacts);

    public class RequestSummaryProjection : IProjectionFor<RequestSummary>
    {
        public void Define(IProjectionBuilderFor<RequestSummary> builder) => builder
            .Children(m => m.Contacts, c => c
                .IdentifiedBy(child => child.ContactId)
                .From<ContactAdded>(_ => _
                    .{|#0:UsingParentKeyFromContext|}(ctx => ctx.CorrelationId)
                    .Set(child => child.EmailAddress).To(e => e.EmailAddress)));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentKeyRedirectionPiiAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.KeyRedirectionPii, DiagnosticSeverity.Warning, "EmailAddress", "ContactAdded", "EmailAddress", "EventContext.CorrelationId", "ContactId"));

    [Fact] Task should_report_the_key_redirection_pii_diagnostic() => _result;
}
