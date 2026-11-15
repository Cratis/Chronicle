// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_BatchMethodAnalyzer.when_analyzing_batch_methods;

public class and_a_batch_method_takes_an_optional_parameter : Specification
{
    [Fact]
    Task should_report_batch_method_signature_must_match_allowed() => AnalyzerVerifier<CodeAnalysis.Analyzers.BatchMethodAnalyzer>.VerifyAnalyzer(
        BatchObserverSource.Create("""
            public class Users : IReactor
            {
                [Observes<UserRegistered>]
                public Task {|#0:Index|}(EventBatch batch, BatchContext? context = null) => Task.CompletedTask;
            }
            """),
        new ExpectedDiagnostic(DiagnosticIds.BatchMethodSignatureMustMatchAllowed, DiagnosticSeverity.Error, "Index", "only a CancellationToken may be optional"));
}
