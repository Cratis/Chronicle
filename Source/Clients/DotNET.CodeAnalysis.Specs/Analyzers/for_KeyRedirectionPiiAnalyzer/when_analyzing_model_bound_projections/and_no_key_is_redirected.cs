// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_KeyRedirectionPiiAnalyzer.when_analyzing_model_bound_projections;

/// <summary>
/// Without a key argument the document is keyed by the stream the event was appended to, which is where the PII already belongs.
/// </summary>
public class and_no_key_is_redirected : given.a_key_redirection_pii_analyzer
{
    const string Usage = """
    public record AdvisorNamed([PII] string FullName);

    [FromEvent<AdvisorNamed>]
    public record AdvisorSummary(
        [Key] string Id,
        string FullName);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.KeyRedirectionPiiAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
