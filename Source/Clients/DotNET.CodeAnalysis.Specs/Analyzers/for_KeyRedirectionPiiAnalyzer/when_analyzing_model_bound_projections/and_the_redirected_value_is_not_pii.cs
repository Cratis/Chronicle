// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_KeyRedirectionPiiAnalyzer.when_analyzing_model_bound_projections;

/// <summary>
/// Redirection on its own is an ordinary, supported shape - it is only the personal value riding along that this rule is about.
/// </summary>
public class and_the_redirected_value_is_not_pii : given.a_key_redirection_pii_analyzer
{
    const string Usage = """
    public record AdvisorNamed(string RequestId, string Department);

    [FromEvent<AdvisorNamed>(key: "RequestId")]
    public record RequestSummary(
        [Key] string Id,
        string Department);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.KeyRedirectionPiiAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
