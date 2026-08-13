// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_PassiveProjectionKeyRedirectionAnalyzer.when_analyzing_a_passive_read_model;

/// <summary>
/// An active read model is materialized by an observer, so the redirected document exists before a read.
/// </summary>
public class and_the_read_model_has_a_sink : given.a_passive_projection_key_redirection_analyzer
{
    const string Usage = """
    public record UserSignedUp(string Hash, string Region);

    [FromEvent<UserSignedUp>(key: "Hash")]
    public record UserByHash(
        [Key] string Id,
        string Region);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.PassiveProjectionKeyRedirectionAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
