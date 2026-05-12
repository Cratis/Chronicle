// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ModelBoundProjectionMultipleEventStoresAnalyzer.when_analyzing_model_bound_projection_types;

public class and_one_event_type_uses_the_default_store_and_another_uses_an_explicit_store : given.a_model_bound_projection_multiple_event_stores_analyzer
{
    const string Usage = """
    [EventType]
    public class EventFromDefaultStore { }

    [EventType]
    [EventStore("event-store-two")]
    public class EventFromExplicitStore { }

    {|#0:[FromEvent<EventFromDefaultStore>]
    [FromEvent<EventFromExplicitStore>]
    public class ProjectionWithMixedEventStores { }|}
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ModelBoundProjectionMultipleEventStoresAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.ModelBoundProjectionEventTypesMustBeFromSameEventStore, DiagnosticSeverity.Error, "ProjectionWithMixedEventStores", "<default>", "event-store-two"));

    [Fact] Task should_report_multiple_event_stores_diagnostic() => _result;
}
