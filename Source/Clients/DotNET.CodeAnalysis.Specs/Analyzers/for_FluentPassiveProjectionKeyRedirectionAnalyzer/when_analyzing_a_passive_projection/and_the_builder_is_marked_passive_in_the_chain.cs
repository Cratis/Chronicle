// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentPassiveProjectionKeyRedirectionAnalyzer.when_analyzing_a_passive_projection;

/// <summary>
/// Passivity declared fluently on the same Define chain is the same hazard as the attribute.
/// </summary>
public class and_the_builder_is_marked_passive_in_the_chain : given.a_fluent_passive_projection_key_redirection_analyzer
{
    const string Usage = """
    public record UserSignedUp(string Hash, string Region);

    public record UserByHash(
        [Key] string Id,
        string Region);

    public class UserByHashProjection : IProjectionFor<UserByHash>
    {
        public void Define(IProjectionBuilderFor<UserByHash> builder) => builder
            .Passive()
            .From<UserSignedUp>(_ => _
                .{|#0:UsingKey|}(e => e.Hash)
                .Set(m => m.Region).To(e => e.Region));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentPassiveProjectionKeyRedirectionAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.PassiveProjectionKeyRedirection, DiagnosticSeverity.Warning, "UsingKey", "UserByHash", "Hash", "UserSignedUp"));

    [Fact] Task should_report_the_passive_key_redirection_diagnostic() => _result;
}
