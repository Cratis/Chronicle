// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_EventTypeNullablePropertyAnalyzer.when_analyzing_event_type;

public class and_type_does_not_have_event_type_attribute : given.an_event_type_nullable_property_analyzer
{
    const string Usage = """
    public class MyEvent
    {
        public string Name { get; init; } = string.Empty;
        public int? Count { get; init; }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.EventTypeNullablePropertyAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage));

    [Fact] Task should_not_report_diagnostics() => _result;
}
