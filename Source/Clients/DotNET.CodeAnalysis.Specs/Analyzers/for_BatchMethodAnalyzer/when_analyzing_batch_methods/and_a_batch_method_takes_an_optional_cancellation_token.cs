// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_BatchMethodAnalyzer.when_analyzing_batch_methods;

public class and_a_batch_method_takes_an_optional_cancellation_token : Specification
{
    [Fact]
    Task should_not_report_a_diagnostic() => AnalyzerVerifier<CodeAnalysis.Analyzers.BatchMethodAnalyzer>.VerifyAnalyzer(
        BatchObserverSource.Create("""
            public class Users : IReactor
            {
                [Observes<UserRegistered>]
                public Task Index(EventBatch batch, BatchContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
            }

            public record User(string Name);

            public class UserReducer : IReducerFor<User>
            {
                [Observes<UserRegistered>]
                public User? Reduce(EventBatch events, User? current, CancellationToken cancellationToken = default) => current;
            }
            """));
}
