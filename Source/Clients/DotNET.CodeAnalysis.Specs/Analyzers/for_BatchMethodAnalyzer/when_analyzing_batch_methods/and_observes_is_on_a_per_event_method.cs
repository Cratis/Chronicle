// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_BatchMethodAnalyzer.when_analyzing_batch_methods;

public class and_observes_is_on_a_per_event_method : Specification
{
    [Fact]
    Task should_report_observes_only_on_batch_methods() => AnalyzerVerifier<CodeAnalysis.Analyzers.BatchMethodAnalyzer>.VerifyAnalyzer(
        BatchObserverSource.Create("""
            public class Users : IReactor
            {
                [{|#0:Observes<UserRenamed>|}]
                public Task Registered(UserRegistered @event, EventContext context) => Task.CompletedTask;
            }
            """),
        new ExpectedDiagnostic(DiagnosticIds.ObservesOnlyOnBatchMethods, DiagnosticSeverity.Error, "Registered", "UserRenamed"));
}
