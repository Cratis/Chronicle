// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_BatchMethodAnalyzer.when_analyzing_batch_methods;

public class and_a_self_reducing_models_only_batch_method_is_an_instance_method : Specification
{
    [Fact]
    Task should_report_batch_method_signature_must_match_allowed() => AnalyzerVerifier<CodeAnalysis.Analyzers.BatchMethodAnalyzer>.VerifyAnalyzer(
        BatchObserverSource.Create("""
            public record User(string Name)
            {
                [Observes<UserRegistered>]
                public User? {|#0:Reduce|}(EventBatch events, User? current, BatchContext context) => current;
            }
            """),
        new ExpectedDiagnostic(DiagnosticIds.BatchMethodSignatureMustMatchAllowed, DiagnosticSeverity.Error, "Reduce", "public and static"));
}
