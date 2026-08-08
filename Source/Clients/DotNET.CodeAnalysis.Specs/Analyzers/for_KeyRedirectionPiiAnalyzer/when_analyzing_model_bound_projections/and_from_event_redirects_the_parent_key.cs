// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_KeyRedirectionPiiAnalyzer.when_analyzing_model_bound_projections;

/// <summary>
/// A parent key redirects the document the child's values come to rest on.
/// </summary>
public class and_from_event_redirects_the_parent_key : given.a_key_redirection_pii_analyzer
{
    const string Usage = """
    public record ContactAdded(string RequestId, [PII] string EmailAddress);

    {|#0:[FromEvent<ContactAdded>(parentKey: "RequestId")]|}
    public record Contact(
        [Key] string ContactId,
        string EmailAddress);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.KeyRedirectionPiiAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.KeyRedirectionPii, DiagnosticSeverity.Warning, "EmailAddress", "ContactAdded", "EmailAddress", "RequestId", "ContactId"));

    [Fact] Task should_report_the_key_redirection_pii_diagnostic() => _result;
}
