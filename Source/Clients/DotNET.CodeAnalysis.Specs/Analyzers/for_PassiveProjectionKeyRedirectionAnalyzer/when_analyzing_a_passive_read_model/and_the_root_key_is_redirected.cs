// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_PassiveProjectionKeyRedirectionAnalyzer.when_analyzing_a_passive_read_model;

/// <summary>
/// A passive read model keyed off the event source cannot be rebuilt by a single stream replay.
/// </summary>
public class and_the_root_key_is_redirected : given.a_passive_projection_key_redirection_analyzer
{
    const string Usage = """
    public record UserSignedUp(string Hash, string Region);

    [Passive]
    [{|#0:FromEvent<UserSignedUp>(key: "Hash")|}]
    public record UserByHash(
        [Key] string Id,
        string Region);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.PassiveProjectionKeyRedirectionAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.PassiveProjectionKeyRedirection, DiagnosticSeverity.Warning, "[FromEvent(key:)]", "UserByHash", "Hash", "UserSignedUp"));

    [Fact] Task should_report_the_passive_key_redirection_diagnostic() => _result;
}
