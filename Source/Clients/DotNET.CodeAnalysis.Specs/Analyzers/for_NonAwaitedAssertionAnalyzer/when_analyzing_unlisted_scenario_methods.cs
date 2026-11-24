// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_NonAwaitedAssertionAnalyzer;

public class when_analyzing_unlisted_scenario_methods : given.scenario_terminal_calls
{
    [Theory]
    [InlineData("reactor.Events<int>(new object());")]
    [InlineData("reactor.Events(42);")]
    [InlineData("scenario.Redeliver(new object());")]
    [InlineData("given.Events<int>();")]
    [InlineData("when.Events();")]
    [InlineData("new OtherBuilder().Events();")]
    [InlineData("new Contoso.EventSourceGivenBuilder().Events();")]
    public Task should_not_warn(string statement) =>
        AnalyzerVerifier<CodeAnalysis.Analyzers.NonAwaitedAssertionAnalyzer>.VerifyAnalyzer(
            CreateSource("void Run() { " + statement + " }") + """

            namespace Contoso
            {
                public class EventSourceGivenBuilder
                {
                    public Task Events(params object[] events) => Task.CompletedTask;
                }
            }
            """);

    [Theory]
    [InlineData("params object[] events", "object[] events")]
    [InlineData("params object[] events", "params string[] events")]
    public Task should_require_the_declared_parameter_signature(string original, string replacement) =>
        AnalyzerVerifier<CodeAnalysis.Analyzers.NonAwaitedAssertionAnalyzer>.VerifyAnalyzer(
            CreateSource("void Run() { given.Events([]); }").Replace(original, replacement, StringComparison.Ordinal));

    [Fact]
    public Task should_ignore_missing_testing_types() =>
        AnalyzerVerifier<CodeAnalysis.Analyzers.NonAwaitedAssertionAnalyzer>.VerifyAnalyzer("""
            using System.Threading.Tasks;
            class Spec
            {
                Task Events(params object[] events) => Task.CompletedTask;
                void Run() { Events(); }
            }
            """);
}
