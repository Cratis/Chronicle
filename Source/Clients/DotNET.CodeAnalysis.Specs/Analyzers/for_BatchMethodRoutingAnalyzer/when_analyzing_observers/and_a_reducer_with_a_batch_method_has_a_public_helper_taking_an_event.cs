// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_BatchMethodRoutingAnalyzer.when_analyzing_observers;

public class and_a_reducer_with_a_batch_method_has_a_public_helper_taking_an_event : Specification
{
    [Fact]
    Task should_not_report_a_diagnostic() => AnalyzerVerifier<CodeAnalysis.Analyzers.BatchMethodRoutingAnalyzer>.VerifyAnalyzer(
        BatchObserverSource.Create("""
            public record User(string Name);

            public class UserReducer : IReducerFor<User>
            {
                [Observes<UserRegistered>]
                public User? Reduce(EventBatch events, User? current, BatchContext context) => current;

                public string Describe(UserRegistered @event) => "registered";
            }
            """));
}
