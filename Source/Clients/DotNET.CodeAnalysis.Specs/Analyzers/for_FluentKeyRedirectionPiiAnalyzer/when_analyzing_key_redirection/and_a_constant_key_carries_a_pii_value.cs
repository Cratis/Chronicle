// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentKeyRedirectionPiiAnalyzer.when_analyzing_key_redirection;

/// <summary>
/// A constant key is the starkest form: every person's personal data lands on one document whose subject is a literal.
/// </summary>
public class and_a_constant_key_carries_a_pii_value : given.a_fluent_key_redirection_pii_analyzer
{
    const string Usage = """
    public record AdvisorNamed([PII] string FullName);

    public record Directory(
        [Key] string Id,
        string AdvisorName);

    public class DirectoryProjection : IProjectionFor<Directory>
    {
        public void Define(IProjectionBuilderFor<Directory> builder) => builder
            .From<AdvisorNamed>(_ => _
                .{|#0:UsingConstantKey|}("all-advisors")
                .Set(m => m.AdvisorName).To(e => e.FullName));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentKeyRedirectionPiiAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.KeyRedirectionPii, DiagnosticSeverity.Warning, "AdvisorName", "AdvisorNamed", "FullName", "the constant 'all-advisors'", "Id"));

    [Fact] Task should_report_the_key_redirection_pii_diagnostic() => _result;
}
