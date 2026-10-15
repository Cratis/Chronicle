// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_EventStreamReadModelAnalyzer;

public class when_a_stream_model_is_keyed_by_stream_id : given.stream_declarations
{
    [Fact] public Task should_report_the_stream_key() =>
        AnalyzerVerifier<CodeAnalysis.Analyzers.EventStreamReadModelAnalyzer>.VerifyAnalyzer(
            Source("""
            [EventStream<BankAccount>("Transactions")] public record State(string Id);
            public class Projection
            {
                public void Define(KeyBuilder<State, object, object> builder) => {|#0:builder.UsingKeyFromContext(context => context.EventStreamId)|};
            }
            """),
            new ExpectedDiagnostic(DiagnosticIds.InvalidEventStreamReadModel, DiagnosticSeverity.Error, "State"));
}
