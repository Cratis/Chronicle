// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_BatchMethodRoutingAnalyzer.when_analyzing_observers;

public class and_a_reducer_with_a_batch_method_has_a_private_reduce_method : Specification
{
    [Fact]
    Task should_report_reducer_batch_method_must_be_its_only_method() => AnalyzerVerifier<CodeAnalysis.Analyzers.BatchMethodRoutingAnalyzer>.VerifyAnalyzer(
        BatchObserverSource.Create("""
            public record User(string Name);

            public class UserReducer : IReducerFor<User>
            {
                [Observes<UserRegistered>]
                public User? {|#0:Reduce|}(EventBatch events, User? current, BatchContext context) => current;

                User? Renamed(UserRenamed @event, User? current) => current;
            }
            """),
        new ExpectedDiagnostic(DiagnosticIds.ReducerBatchMethodMustBeItsOnlyMethod, DiagnosticSeverity.Error, "UserReducer", "Renamed"));
}
