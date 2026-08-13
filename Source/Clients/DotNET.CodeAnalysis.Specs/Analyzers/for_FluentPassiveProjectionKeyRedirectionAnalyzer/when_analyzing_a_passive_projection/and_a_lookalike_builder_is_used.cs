// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentPassiveProjectionKeyRedirectionAnalyzer.when_analyzing_a_passive_projection;

/// <summary>
/// A consumer type that merely shares the builder names must never activate a Chronicle rule.
/// </summary>
public class and_a_lookalike_builder_is_used : given.a_fluent_passive_projection_key_redirection_analyzer
{
    const string Usage = """
    public record UserSignedUp(string Hash, string Region);

    [Passive]
    public record UserByHash(
        [Key] string Id,
        string Region);

    public interface IMyBuilder<TReadModel, TEvent>
    {
        IMyBuilder<TReadModel, TEvent> UsingKey<TProperty>(Func<TEvent, TProperty> keyAccessor);
    }

    public class NotAProjection
    {
        public void Define(IMyBuilder<UserByHash, UserSignedUp> builder) => builder
            .UsingKey(e => e.Hash);
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentPassiveProjectionKeyRedirectionAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
