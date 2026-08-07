// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_KeyRedirectionPiiAnalyzer.when_analyzing_model_bound_projections;

/// <summary>
/// [ChildrenFrom]'s key identifies the child inside the collection; it does not move the document, so it is not a redirection.
/// </summary>
public class and_children_from_only_identifies_the_child : given.a_key_redirection_pii_analyzer
{
    const string Usage = """
    public record ContactAdded(string ContactId, [PII] string EmailAddress);

    public record Contact(
        [Key] string ContactId,
        string EmailAddress);

    public record RequestSummary(
        [Key] string Id,
        [ChildrenFrom<ContactAdded>(key: "ContactId", identifiedBy: "ContactId")] IEnumerable<Contact> Contacts);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.KeyRedirectionPiiAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
