// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentPassiveProjectionKeyRedirectionAnalyzer.when_analyzing_a_passive_projection;

/// <summary>
/// A reducer declares no projection, so there is no document key for this rule to read.
/// </summary>
public class and_the_passive_read_model_is_backed_by_a_reducer : given.a_fluent_passive_projection_key_redirection_analyzer
{
    const string Usage = """
    public record UserSignedUp(string Hash, string Region);

    [Passive]
    public record UserByHash(
        [Key] string Id,
        string Region);

    public class UserByHashReducer : IReducerFor<UserByHash>
    {
        public UserByHash Reduce(UserSignedUp @event, UserByHash current) => current;
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentPassiveProjectionKeyRedirectionAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
