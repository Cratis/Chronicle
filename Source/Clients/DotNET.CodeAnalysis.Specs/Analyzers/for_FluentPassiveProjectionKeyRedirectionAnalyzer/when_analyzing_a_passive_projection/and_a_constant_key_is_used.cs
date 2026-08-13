// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentPassiveProjectionKeyRedirectionAnalyzer.when_analyzing_a_passive_projection;

/// <summary>
/// A constant key names one document that no single stream replay will ever build.
/// </summary>
public class and_a_constant_key_is_used : given.a_fluent_passive_projection_key_redirection_analyzer
{
    const string Usage = """
    public record UserSignedUp(string Region);

    [Passive]
    public record UserTotals(
        [Key] string Id,
        string Region);

    public class UserTotalsProjection : IProjectionFor<UserTotals>
    {
        public void Define(IProjectionBuilderFor<UserTotals> builder) => builder
            .From<UserSignedUp>(_ => _
                .{|#0:UsingConstantKey|}("all")
                .Set(m => m.Region).To(e => e.Region));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentPassiveProjectionKeyRedirectionAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.PassiveProjectionKeyRedirection, DiagnosticSeverity.Warning, "UsingConstantKey", "UserTotals", "the constant 'all'", "UserSignedUp"));

    [Fact] Task should_report_the_passive_key_redirection_diagnostic() => _result;
}
