// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_BatchMethodAnalyzer.when_analyzing_batch_methods;

public class and_a_reactor_batch_method_returns_events : Specification
{
    [Fact]
    Task should_report_reactor_batch_method_must_return_task_or_void() => AnalyzerVerifier<CodeAnalysis.Analyzers.BatchMethodAnalyzer>.VerifyAnalyzer(
        BatchObserverSource.Create("""
            public class Users : IReactor
            {
                [Observes<UserRegistered>]
                public IEnumerable<object> {|#0:Index|}(EventBatch batch, BatchContext context) => [];
            }
            """),
        new ExpectedDiagnostic(DiagnosticIds.ReactorBatchMethodMustReturnTaskOrVoid, DiagnosticSeverity.Error, "Index", "IEnumerable<object>"));
}
