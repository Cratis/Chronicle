// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_EventStreamReadModelAnalyzer;

public class when_the_declaration_is_valid : given.stream_declarations
{
    [Theory]
    [InlineData("[EventStream<BankAccount>(\"Transactions\")] public record State(string Id);")]
    [InlineData("public record State(string Id);")]
    [InlineData("[EventStream<BankAccount>(\"Transactions\")] public record State(string Id); public class Projection { public void Define(KeyBuilder<State, object, object> builder) => builder.UsingKeyFromContext(context => context.EventSourceId); }")]
    [InlineData("[EventStream<BankAccount>(\"Transactions\")] public record State(string Id); public record Child(string Id); public class Projection { public void Define(KeyBuilder<Child, object, object> builder) => builder.UsingKeyFromContext(context => context.EventStreamId); }")]
    public Task should_not_report_valid_or_unrelated_types(string declaration) =>
        AnalyzerVerifier<CodeAnalysis.Analyzers.EventStreamReadModelAnalyzer>.VerifyAnalyzer(Source(declaration));
}
