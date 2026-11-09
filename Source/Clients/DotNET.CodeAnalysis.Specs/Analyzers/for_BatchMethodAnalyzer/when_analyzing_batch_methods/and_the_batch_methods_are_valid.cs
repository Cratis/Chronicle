// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_BatchMethodAnalyzer.when_analyzing_batch_methods;

public class and_the_batch_methods_are_valid : Specification
{
    public const string Source = """
        public class Users : IReactor
        {
            [Observes<UserRegistered>]
            [Observes<UserRenamed>]
            public Task Index(EventBatch batch, BatchContext context, CancellationToken cancellationToken, ISearchIndex other) =>
                Task.CompletedTask;

            public void Removed(UserRemoved @event, EventContext context) { }
        }

        public record User(string Name);

        public class UserReducer : IReducerFor<User>
        {
            [Observes<UserRegistered>]
            [Observes<UserRenamed>]
            public Task<User?> Reduce(EventBatch events, User? current, BatchContext context) => Task.FromResult(current);
        }

        public record Account(decimal Balance)
        {
            [Observes<UserRegistered>]
            [Observes<UserRemoved>]
            public static Account? Reduce(EventBatch events, Account? current, BatchContext context)
            {
                foreach (var e in events)
                {
                    current = e.Event switch
                    {
                        UserRegistered => new Account(0),
                        UserRemoved => null,
                        _ => current
                    };
                }

                return current;
            }
        }

        public static class Helpers
        {
            public static int Count(EventBatch batch, int offset) => batch.Count + offset;
        }
        """;

    [Fact] Task should_not_report_a_diagnostic() => AnalyzerVerifier<CodeAnalysis.Analyzers.BatchMethodAnalyzer>.VerifyAnalyzer(BatchObserverSource.Create(Source));
}
