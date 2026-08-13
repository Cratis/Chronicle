// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentPassiveProjectionKeyRedirectionAnalyzer.when_analyzing_a_passive_projection;

/// <summary>
/// A context member other than the event source id routes the document off the event stream.
/// </summary>
public class and_the_key_comes_from_a_context_member : given.a_fluent_passive_projection_key_redirection_analyzer
{
    const string Usage = """
    public record UserSignedUp(string Region);

    [Passive]
    public record UserByCorrelation(
        [Key] string Id,
        string Region);

    public class UserByCorrelationProjection : IProjectionFor<UserByCorrelation>
    {
        public void Define(IProjectionBuilderFor<UserByCorrelation> builder) => builder
            .From<UserSignedUp>(_ => _
                .{|#0:UsingKeyFromContext|}(c => c.CorrelationId)
                .Set(m => m.Region).To(e => e.Region));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentPassiveProjectionKeyRedirectionAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.PassiveProjectionKeyRedirection, DiagnosticSeverity.Warning, "UsingKeyFromContext", "UserByCorrelation", "EventContext.CorrelationId", "UserSignedUp"));

    [Fact] Task should_report_the_passive_key_redirection_diagnostic() => _result;
}
