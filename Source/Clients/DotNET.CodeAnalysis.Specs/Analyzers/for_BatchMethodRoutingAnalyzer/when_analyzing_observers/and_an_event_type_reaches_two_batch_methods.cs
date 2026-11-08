// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_BatchMethodRoutingAnalyzer.when_analyzing_observers;

public class and_an_event_type_reaches_two_batch_methods : Specification
{
    [Fact]
    Task should_report_event_type_must_reach_one_method() => AnalyzerVerifier<CodeAnalysis.Analyzers.BatchMethodRoutingAnalyzer>.VerifyAnalyzer(
        BatchObserverSource.Create("""
            public class Users : IReactor
            {
                [Observes<UserRegistered>]
                public Task Index(EventBatch batch, BatchContext context) => Task.CompletedTask;

                [{|#0:Observes<UserRegistered>|}]
                [Observes<UserRenamed>]
                public Task Notify(EventBatch batch, BatchContext context) => Task.CompletedTask;
            }
            """),
        new ExpectedDiagnostic(DiagnosticIds.EventTypeMustReachOneMethod, DiagnosticSeverity.Error, "UserRegistered", "Index", "Notify"));
}
