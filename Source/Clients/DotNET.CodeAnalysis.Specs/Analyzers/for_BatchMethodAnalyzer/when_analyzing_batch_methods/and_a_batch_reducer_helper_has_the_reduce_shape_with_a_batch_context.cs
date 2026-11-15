// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_BatchMethodAnalyzer.when_analyzing_batch_methods;

public class and_a_batch_reducer_helper_has_the_reduce_shape_with_a_batch_context : Specification
{
    [Fact]
    Task should_not_report_a_diagnostic() => AnalyzerVerifier<CodeAnalysis.Analyzers.BatchMethodAnalyzer>.VerifyAnalyzer(
        BatchObserverSource.Create("""
            public record User(string Name);

            public class UserReducer : IReducerFor<User>
            {
                [Observes<UserRegistered>]
                public User? Reduce(EventBatch events, User? current, BatchContext context)
                {
                    foreach (var e in events)
                    {
                        current = Apply((UserRegistered)e.Event, current, context);
                    }

                    return current;
                }

                User? Apply(UserRegistered @event, User? current, BatchContext context) => current;
            }
            """));
}
