// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentKeyRedirectionPiiAnalyzer.when_analyzing_key_redirection;

public class and_explicit_this_qualification_shares_automap_state : given.a_fluent_key_redirection_pii_analyzer
{
    const string Usage = """
    public record AdvisorNamed(string RequestId, [PII] string FullName);

    [NoAutoMap]
    public record RequestSummary([Key] string Id, string FullName);

    public class BuilderHolder
    {
        public IProjectionBuilderFor<RequestSummary> Enabled { get; } = default!;
    }

    public class RequestSummaryProjection : IProjectionFor<RequestSummary>
    {
        readonly BuilderHolder _holder = new();

        public void Define(IProjectionBuilderFor<RequestSummary> builder)
        {
            this._holder.Enabled.AutoMap();
            _holder.Enabled.From<AdvisorNamed>(from => from.{|#0:UsingKey|}(e => e.RequestId));
        }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentKeyRedirectionPiiAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.KeyRedirectionPii, DiagnosticSeverity.Warning, "FullName", "AdvisorNamed", "RequestId", "Id"));

    [Fact] Task should_report_the_key_redirection_on_the_enabled_builder() => _result;
}
