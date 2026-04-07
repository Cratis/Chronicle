// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ModelBoundProjectionMultipleEventStoresAnalyzer.when_analyzing_model_bound_projection_types;

public class and_all_event_types_have_the_same_event_store : given.a_model_bound_projection_multiple_event_stores_analyzer
{
    const string Usage = """
    [EventType]
    [EventStore("event-store-one")]
    public class EventFromFirstStore { }

    [EventType]
    [EventStore("event-store-one")]
    public class AnotherEventFromSameStore { }

    [FromEvent<EventFromFirstStore>]
    [FromEvent<AnotherEventFromSameStore>]
    public class ValidProjection { }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ModelBoundProjectionMultipleEventStoresAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
