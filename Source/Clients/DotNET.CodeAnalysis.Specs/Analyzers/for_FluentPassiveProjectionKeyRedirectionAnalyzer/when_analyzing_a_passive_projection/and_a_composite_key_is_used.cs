// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentPassiveProjectionKeyRedirectionAnalyzer.when_analyzing_a_passive_projection;

/// <summary>
/// A composite key canonicalizes to a joined string, which is never an event source id.
/// </summary>
public class and_a_composite_key_is_used : given.a_fluent_passive_projection_key_redirection_analyzer
{
    const string Usage = """
    public record UserSignedUp(string Region, string RequestId);

    public record RequestKey(string Region, string RequestId);

    [Passive]
    public record RequestSummary(
        [Key] string Id,
        string Region);

    public class RequestSummaryProjection : IProjectionFor<RequestSummary>
    {
        public void Define(IProjectionBuilderFor<RequestSummary> builder) => builder
            .From<UserSignedUp>(_ => _
                .{|#0:UsingCompositeKey<RequestKey>|}(k => k
                    .Set(key => key.Region).To(e => e.Region)
                    .Set(key => key.RequestId).To(e => e.RequestId))
                .Set(m => m.Region).To(e => e.Region));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentPassiveProjectionKeyRedirectionAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.PassiveProjectionKeyRedirection, DiagnosticSeverity.Warning, "UsingCompositeKey", "RequestSummary", "a composite key", "UserSignedUp"));

    [Fact] Task should_report_the_passive_key_redirection_diagnostic() => _result;
}
