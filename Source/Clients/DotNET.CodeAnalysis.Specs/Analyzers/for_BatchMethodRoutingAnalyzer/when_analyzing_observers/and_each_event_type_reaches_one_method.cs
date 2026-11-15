// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_BatchMethodRoutingAnalyzer.when_analyzing_observers;

public class and_each_event_type_reaches_one_method : Specification
{
    [Fact]
    Task should_not_report_a_diagnostic() => AnalyzerVerifier<CodeAnalysis.Analyzers.BatchMethodRoutingAnalyzer>.VerifyAnalyzer(
        BatchObserverSource.Create("""
            public class Users : IReactor
            {
                [Observes<UserRegistered>]
                [Observes<UserRenamed>]
                public Task Index(EventBatch batch, BatchContext context) => Task.CompletedTask;

                [Observes<UserRemoved>]
                public Task Forget(EventBatch batch, BatchContext context) => Task.CompletedTask;
            }

            public class Notifications : IReactor
            {
                [Observes<UserRegistered>]
                public Task Welcome(EventBatch batch, BatchContext context) => Task.CompletedTask;

                public Task Renamed(UserRenamed @event, EventContext context) => Task.CompletedTask;
            }
            """));
}
