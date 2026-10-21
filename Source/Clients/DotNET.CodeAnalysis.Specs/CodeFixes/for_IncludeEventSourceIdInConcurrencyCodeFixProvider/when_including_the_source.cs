// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.CodeFixes.for_IncludeEventSourceIdInConcurrencyCodeFixProvider;

public class when_including_the_source : Analyzers.for_ConcurrencyWithoutEventSourceIdAnalyzer.given.concurrency_declarations
{
    Task _result;

    void Because() => _result = Task.WhenAll(new[] { "EventSource(", "EventStream(\"Approval\", " }.Select(attribute =>
        CodeFixVerifier<CodeAnalysis.Analyzers.ConcurrencyWithoutEventSourceIdAnalyzer, CodeAnalysis.CodeFixes.IncludeEventSourceIdInConcurrencyCodeFixProvider>.VerifyCodeFix(
            Source($"[{attribute}Concurrency = {{|#0:ConcurrencyDimensions.EventStreamType | ConcurrencyDimensions.EventStreamId|}})] public class Account : IEventSource {{ }}"),
            Source($"[{attribute}Concurrency = ConcurrencyDimensions.EventStreamType | ConcurrencyDimensions.EventStreamId | Dimensions.EventSourceId)] public class Account : IEventSource {{ }}"),
            new ExpectedDiagnostic(DiagnosticIds.ConcurrencyWithoutEventSourceId, DiagnosticSeverity.Info))));

    [Fact] Task should_include_the_source_without_dropping_existing_dimensions() => _result;
}
