// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_EventStreamReadModelAnalyzer;

public class when_the_attribute_is_misused : given.stream_declarations
{
    [Theory]
    [InlineData("[{|#0:EventStream<BankAccount>(\"Missing\")|}] public record State(string Id);")]
    [InlineData("[{|#0:EventStream<BankAccount>(\"All\")|}] public record State(string Id);")]
    [InlineData("[{|#0:Cratis.Chronicle.EventSources.EventStream(\"Transactions\")|}] public record State(string Id);")]
    [InlineData("[{|#0:EventStream<BankAccount>(\"Transactions\")|}] public class State : IEventSource {}")]
    [InlineData("[{|#0:EventStream<BankAccount>(\"Transactions\")|}] public record State(string Id) { public static State Reduce(Fact e, State? current) => new State(\"id\"); } public record Fact;")]
    [InlineData("[{|#0:EventStream<BankAccount>(\"Transactions\")|}] public record State(string Id); public class Reducer : Cratis.Chronicle.Reducers.IReducerFor<State> {}")]
    public Task should_report_invalid_declarations(string declaration) =>
        AnalyzerVerifier<CodeAnalysis.Analyzers.EventStreamReadModelAnalyzer>.VerifyAnalyzer(Source(declaration),
            new ExpectedDiagnostic(DiagnosticIds.InvalidEventStreamReadModel, DiagnosticSeverity.Error, "State"));
}
