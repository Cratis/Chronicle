// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ModelBoundProjectionMultipleEventStoresAnalyzer.when_analyzing_model_bound_projection_types;

public class and_event_types_have_different_event_stores : given.a_model_bound_projection_multiple_event_stores_analyzer
{
    const string Usage = """
    [EventType]
    [EventStore("event-store-one")]
    public class EventFromFirstStore { }

    [EventType]
    [EventStore("event-store-two")]
    public class EventFromSecondStore { }

    {|#0:[FromEvent<EventFromFirstStore>]
    [FromEvent<EventFromSecondStore>]
    public class ProjectionWithMixedEventStores { }|}
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ModelBoundProjectionMultipleEventStoresAnalyzer>.VerifyAnalyzer(CreateSource(Usage), new ExpectedDiagnostic(DiagnosticIds.ModelBoundProjectionEventTypesMustBeFromSameEventStore, DiagnosticSeverity.Error, "ProjectionWithMixedEventStores", "event-store-one", "event-store-two"));

    [Fact] Task should_report_multiple_event_stores_diagnostic() => _result;
}
