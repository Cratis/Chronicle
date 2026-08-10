// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_KeyRedirectionPiiAnalyzer.when_analyzing_model_bound_projections;

/// <summary>
/// A [Key] does not control client-side compliance release; without [Subject], release falls back to Id.
/// </summary>
public class and_the_read_model_resolves_its_subject_from_the_key_attribute : given.a_key_redirection_pii_analyzer
{
    const string Usage = """
    public record AdvisorNamed(string RequestId, [PII] string FullName);

    {|#0:[FromEvent<AdvisorNamed>(key: "RequestId")]|}
    public record RequestSummary(
        string Id,
        [Key] string RequestKey,
        string FullName);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.KeyRedirectionPiiAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.KeyRedirectionPii, DiagnosticSeverity.Warning, "FullName", "AdvisorNamed", "FullName", "RequestId", "Id"));

    [Fact] Task should_name_the_client_release_id_fallback() => _result;
}
