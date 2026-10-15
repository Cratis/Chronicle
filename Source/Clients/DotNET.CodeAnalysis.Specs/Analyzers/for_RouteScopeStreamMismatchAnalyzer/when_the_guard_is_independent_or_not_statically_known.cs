// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_RouteScopeStreamMismatchAnalyzer;

public class when_the_guard_is_independent_or_not_statically_known : given.routed_appends
{
    Task _result;

    void Because() => _result = Task.WhenAll(new[]
    {
        "sequence.Append<Account>(\"id\", new object(), \"Onboarding\", scope);",
        "sequence.Append<Other>(\"id\", new object(), \"Transactions\", scope);",
        "sequence.Append<Account>(\"id\", new object(), unknown, scope);",
        "sequence.AppendMany(new object[0], concurrencyScopes: scope);",
        "sequence.AppendPrepared(\"id\", new object(), \"Transactions\", scope);",
        "scope = new ConcurrencyScope(); sequence.Append<Account>(\"id\", new object(), \"Transactions\", scope);",
        "sequence.Append<Account>(\"id\", new object(), \"Transactions\", new ConcurrencyScopeBuilder().ForEventSource<Account>(\"Onboarding\").WithEventStreamType(\"Transactions\").Build());",
        "var builder = new ConcurrencyScopeBuilder().ForEventSource<Account>(\"Onboarding\"); builder.WithEventStreamType(\"Transactions\"); sequence.Append<Account>(\"id\", new object(), \"Transactions\", builder.Build());"
    }.Select(append => AnalyzerVerifier<CodeAnalysis.Analyzers.RouteScopeStreamMismatchAnalyzer>.VerifyAnalyzer(
        Source("var scope = new ConcurrencyScopeBuilder().ForEventSource<Account>(\"Onboarding\").Build();\n" + append))));

    [Fact] Task should_not_guess_the_guard_or_change_an_independent_condition() => _result;
}
