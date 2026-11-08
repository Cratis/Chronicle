// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_BatchMethodPatternAnalyzer.when_analyzing_batch_methods;

public class and_every_match_is_on_an_observed_event_type : Specification
{
    [Fact]
    Task should_not_report_a_diagnostic() => AnalyzerVerifier<CodeAnalysis.Analyzers.BatchMethodPatternAnalyzer>.VerifyAnalyzer(
        BatchObserverSource.Create("""
            public interface IUserEvent {}
            [EventType] public class UserActivated : IUserEvent {}

            public class Users : IReactor
            {
                [Observes<UserRegistered>]
                [Observes<IUserEvent>]
                public Task Index(EventBatch batch, BatchContext context)
                {
                    foreach (var e in batch)
                    {
                        var text = e.Event switch
                        {
                            UserRegistered registered => "registered",
                            UserActivated => "activated",
                            string value => value,
                            _ => "other"
                        };
                    }

                    return Task.CompletedTask;
                }

                public void Removed(UserRemoved @event, EventContext context)
                {
                    if (@event is UserRemoved) { }
                }
            }
            """));
}
