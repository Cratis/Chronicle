// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_BatchMethodAnalyzer.when_analyzing_batch_methods;

public class and_a_reducer_method_takes_a_batch_context : Specification
{
    [Fact]
    Task should_report_batch_context_only_on_batch_methods() => AnalyzerVerifier<CodeAnalysis.Analyzers.BatchMethodAnalyzer>.VerifyAnalyzer(
        BatchObserverSource.Create("""
            public record User(string Name);

            public class UserReducer : IReducerFor<User>
            {
                User? Registered(UserRegistered @event, User? current, BatchContext {|#0:context|}) => current;
            }
            """),
        new ExpectedDiagnostic(DiagnosticIds.BatchContextOnlyOnBatchMethods, DiagnosticSeverity.Error, "Registered"));
}
