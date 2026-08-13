// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentPassiveProjectionKeyRedirectionAnalyzer.when_analyzing_a_passive_projection;

/// <summary>
/// Restating the identity is not a redirection: the replay already walks that stream.
/// </summary>
public class and_the_key_is_the_event_source_id_from_context : given.a_fluent_passive_projection_key_redirection_analyzer
{
    const string Usage = """
    public record UserSignedUp(string Region);

    [Passive]
    public record UserSummary(
        [Key] string Id,
        string Region);

    public class UserSummaryProjection : IProjectionFor<UserSummary>
    {
        public void Define(IProjectionBuilderFor<UserSummary> builder) => builder
            .From<UserSignedUp>(_ => _
                .UsingKeyFromContext(c => c.EventSourceId)
                .Set(m => m.Region).To(e => e.Region));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentPassiveProjectionKeyRedirectionAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
