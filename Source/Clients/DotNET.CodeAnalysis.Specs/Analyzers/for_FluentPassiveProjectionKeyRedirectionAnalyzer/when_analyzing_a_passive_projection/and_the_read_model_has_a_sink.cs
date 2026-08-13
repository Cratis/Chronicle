// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentPassiveProjectionKeyRedirectionAnalyzer.when_analyzing_a_passive_projection;

/// <summary>
/// An active read model is materialized by an observer, so the redirected document is written before any read.
/// </summary>
public class and_the_read_model_has_a_sink : given.a_fluent_passive_projection_key_redirection_analyzer
{
    const string Usage = """
    public record UserSignedUp(string Hash, string Region);

    public record UserByHash(
        [Key] string Id,
        string Region);

    public class UserByHashProjection : IProjectionFor<UserByHash>
    {
        public void Define(IProjectionBuilderFor<UserByHash> builder) => builder
            .From<UserSignedUp>(_ => _
                .UsingKey(e => e.Hash)
                .Set(m => m.Region).To(e => e.Region));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentPassiveProjectionKeyRedirectionAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
