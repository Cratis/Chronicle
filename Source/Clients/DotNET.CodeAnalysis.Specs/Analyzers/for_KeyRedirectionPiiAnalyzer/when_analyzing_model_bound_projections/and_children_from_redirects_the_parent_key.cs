// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_KeyRedirectionPiiAnalyzer.when_analyzing_model_bound_projections;

/// <summary>
/// [ChildrenFrom]'s parentKey decides which document the whole collection lands on, so the child's [PII] follows it.
/// </summary>
public class and_children_from_redirects_the_parent_key : given.a_key_redirection_pii_analyzer
{
    const string Usage = """
    public record ContactAdded(string RequestId, string ContactId, [PII] string EmailAddress);

    public record Contact(
        [Key] string ContactId,
        string EmailAddress);

    public record RequestSummary(
        [Key] string Id,
        {|#0:[ChildrenFrom<ContactAdded>(identifiedBy: "ContactId", parentKey: "RequestId")]|} IEnumerable<Contact> Contacts);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.KeyRedirectionPiiAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.KeyRedirectionPii, DiagnosticSeverity.Warning, "EmailAddress", "ContactAdded", "EmailAddress", "RequestId", "Id"));

    [Fact] Task should_report_the_key_redirection_pii_diagnostic() => _result;
}
