// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ConcurrencyWithoutEventSourceIdAnalyzer;

public class when_concurrency_is_per_source_or_undeclared : given.concurrency_declarations
{
    Task _result;

    void Because() => _result = Task.WhenAll(new[]
    {
        "[EventSource] public class Account : IEventSource { }",
        "[EventSource(Concurrency = Dimensions.None)] public class Account : IEventSource { }",
        "[EventSource(Concurrency = Dimensions.EventSourceId | Dimensions.EventStreamType)] public class Account : IEventSource { }",
        "[EventStream(\"Approval\", Concurrency = Dimensions.EventSourceId | Dimensions.EventStreamId)] public class Account : IEventSource { }",
        "[EventSource(Concurrency = Dimensions.EventStreamType)] public class NotADefinition { }",
        "namespace Other { public class EventSourceAttribute : System.Attribute { public int Concurrency { get; set; } } [EventSource(Concurrency = 4)] public class Account : IEventSource { } }"
    }.Select(usage => AnalyzerVerifier<CodeAnalysis.Analyzers.ConcurrencyWithoutEventSourceIdAnalyzer>.VerifyAnalyzer(Source(usage))));

    [Fact] Task should_not_report_a_cross_source_declaration() => _result;
}
