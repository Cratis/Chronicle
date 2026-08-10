// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_KeyRedirectionPiiAnalyzer.when_analyzing_model_bound_projections;

/// <summary>
/// Recursive&lt;T&gt; grows to Recursive&lt;List&lt;T&gt;&gt;; the repeated original definition must terminate before the later serialized Value member exposes Contact PII.
/// </summary>
public class and_a_recursive_generic_changes_its_construction : given.a_key_redirection_pii_analyzer
{
    const string Usage = """
    [PII]
    public record Contact(string Email);

    public record Recursive<T>(Recursive<List<T>>? Next, T Value);

    public record AdvisorContactsChanged(string RequestId, Recursive<Contact> Details);

    {|#0:[FromEvent<AdvisorContactsChanged>(key: "RequestId")]|}
    public record RequestSummary(
        [Key] string Id,
        Recursive<Contact> Details);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.KeyRedirectionPiiAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.KeyRedirectionPii, DiagnosticSeverity.Warning, "Details", "AdvisorContactsChanged", "RequestId", "Id"));

    [Fact] Task should_report_the_key_redirection_pii_diagnostic() => _result;
}
