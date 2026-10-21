// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_RouteScopeStreamMismatchAnalyzer;

public class when_a_scope_guards_another_stream : given.routed_appends
{
    [Theory]
    [InlineData("sequence.AppendThroughEventSource(typeof(Account), \"id\", new object(), \"Transactions\", {|#0:scope|});")]
    [InlineData("sequence.AppendManyThroughEventSource(typeof(Account), \"id\", new object[0], \"Transactions\", {|#0:scope|});")]
    [InlineData("sequence.AppendPreparedThroughEventSource(typeof(Account), \"id\", new object(), \"Transactions\", {|#0:scope|});")]
    [InlineData("sequence.Append<Account>(\"id\", new object(), eventStream: \"Transactions\", concurrencyScope: {|#0:scope|});")]
    [InlineData("sequence.AppendMany<Account>(\"id\", new object[0], concurrencyScope: {|#0:scope|}, eventStream: \"Transactions\");")]
    [InlineData("sequence.AppendPrepared<Account>(\"id\", new object(), \"Transactions\", {|#0:scope|});")]
    [InlineData("sequence.Append<Account>(\"id\", new object(), \"Transactions\", {|#0:new ConcurrencyScopeBuilder().ForEventSource<Account>(\"Onboarding\").Build()|});")]
    public Task should_report_each_single_route_api_and_extension(string append) =>
        AnalyzerVerifier<CodeAnalysis.Analyzers.RouteScopeStreamMismatchAnalyzer>.VerifyAnalyzer(
            Source("const string first = \"Onboarding\"; var scope = new ConcurrencyScopeBuilder().ForEventSource<Account>(first).WithSequenceNumber(42).Build();\n" + append),
            new ExpectedDiagnostic(DiagnosticIds.RouteScopeStreamMismatch, DiagnosticSeverity.Warning, "Account", "Onboarding", "Transactions"));
}
