// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_BatchMethodPatternAnalyzer.when_analyzing_batch_methods;

public class and_an_is_expression_matches_an_event_type_it_does_not_observe : Specification
{
    [Fact]
    Task should_warn_that_the_match_is_never_reached() => AnalyzerVerifier<CodeAnalysis.Analyzers.BatchMethodPatternAnalyzer>.VerifyAnalyzer(
        BatchObserverSource.Create("""
            public record User(string Name)
            {
                [Observes<UserRegistered>]
                public static User? Reduce(EventBatch events, User? current, BatchContext context)
                {
                    foreach (var e in events)
                    {
                        if ({|#0:e.Event is UserRenamed|})
                        {
                            return current;
                        }
                    }

                    return new("registered");
                }
            }
            """),
        new ExpectedDiagnostic(DiagnosticIds.BatchMethodMatchesUnobservedEventType, DiagnosticSeverity.Warning, "Reduce", "UserRenamed"));
}
