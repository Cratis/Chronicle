// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ModelBoundProjectionMultipleEventStoresAnalyzer.when_analyzing_model_bound_projection_types;

/// <summary>
/// Absence of the attribute is not a store. It means unconstrained - whatever the host is configured with - so an
/// event type naming no store is compatible with any single named one and can never be what makes a projection
/// span two.
/// </summary>
/// <remarks>
/// Counting a synthesized <c>&lt;default&gt;</c> sentinel alongside real names reported the ordinary shape as an
/// error: a host declares its own event types locally, imports a few from a contracts assembly that pins the
/// store name so those events route across hosts, and there is exactly one store in the deployment.
/// <para>
/// This spec previously asserted the opposite, which is to say it pinned the defect.
/// </para>
/// </remarks>
public class and_one_event_type_uses_the_default_store_and_another_uses_an_explicit_store : given.a_model_bound_projection_multiple_event_stores_analyzer
{
    const string Usage = """
    [EventType]
    public class EventFromDefaultStore { }

    [EventType]
    [EventStore("event-store-two")]
    public class EventFromExplicitStore { }

    [FromEvent<EventFromDefaultStore>]
    [FromEvent<EventFromExplicitStore>]
    public class ProjectionWithMixedEventStores { }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ModelBoundProjectionMultipleEventStoresAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_an_unconstrained_event_type_as_a_second_store() => _result;
}
