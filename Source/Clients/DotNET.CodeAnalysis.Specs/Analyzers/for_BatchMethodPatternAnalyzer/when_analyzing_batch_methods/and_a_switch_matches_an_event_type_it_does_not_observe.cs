// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_BatchMethodPatternAnalyzer.when_analyzing_batch_methods;

public class and_a_switch_matches_an_event_type_it_does_not_observe : Specification
{
    [Fact]
    Task should_warn_that_the_case_is_never_reached() => AnalyzerVerifier<CodeAnalysis.Analyzers.BatchMethodPatternAnalyzer>.VerifyAnalyzer(
        BatchObserverSource.Create("""
            public class Users : IReactor
            {
                [Observes<UserRegistered>]
                public Task Index(EventBatch batch, BatchContext context)
                {
                    Func<EventAndContext, string> name = e => e.Event switch
                    {
                        UserRegistered => "registered",
                        {|#0:UserRemoved removed|} => "removed",
                        _ => "unknown"
                    };
                    return Task.CompletedTask;
                }
            }
            """),
        new ExpectedDiagnostic(DiagnosticIds.BatchMethodMatchesUnobservedEventType, DiagnosticSeverity.Warning, "Index", "UserRemoved"));
}
