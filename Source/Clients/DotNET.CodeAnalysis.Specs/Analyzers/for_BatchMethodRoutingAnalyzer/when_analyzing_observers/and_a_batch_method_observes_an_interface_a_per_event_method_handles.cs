// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_BatchMethodRoutingAnalyzer.when_analyzing_observers;

public class and_a_batch_method_observes_an_interface_a_per_event_method_handles : Specification
{
    [Fact]
    Task should_report_event_type_must_reach_one_method() => AnalyzerVerifier<CodeAnalysis.Analyzers.BatchMethodRoutingAnalyzer>.VerifyAnalyzer(
        BatchObserverSource.Create("""
            public interface IAccountEvent {}
            [EventType] public class AccountOpened : IAccountEvent {}

            public class Accounts : IReactor
            {
                [Observes<IAccountEvent>]
                public Task Index(EventBatch batch, BatchContext context) => Task.CompletedTask;

                public Task {|#0:Opened|}(AccountOpened @event, EventContext context) => Task.CompletedTask;
            }
            """),
        new ExpectedDiagnostic(DiagnosticIds.EventTypeMustReachOneMethod, DiagnosticSeverity.Error, "AccountOpened", "Index", "Opened"));
}
