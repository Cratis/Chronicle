// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402, SA1649

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.ReadModels;

namespace Cratis.Chronicle.Testing.for_3434_repros.issue_4081;

/// <summary>
/// An endpoint concept.
/// </summary>
/// <param name="Value">The value.</param>
public record Endpoint4081(string Value) : ConceptAs<string>(Value);

/// <summary>
/// A thing was configured.
/// </summary>
/// <param name="Endpoint">The endpoint.</param>
/// <param name="Name">The name.</param>
[EventType]
public record Thing4081Configured(Endpoint4081 Endpoint, string Name);

/// <summary>
/// Passive read model with a nullable concept property.
/// </summary>
/// <param name="Id">The id.</param>
/// <param name="Endpoint">The endpoint.</param>
/// <param name="Name">The name.</param>
[Passive]
[FromEvent<Thing4081Configured>]
public record Thing4081(
    Guid Id,
    [SetFrom<Thing4081Configured>(nameof(Thing4081Configured.Endpoint))]
    Endpoint4081? Endpoint = null,
    [SetFrom<Thing4081Configured>(nameof(Thing4081Configured.Name))]
    string Name = "");

public class when_projecting_a_nullable_property_on_a_passive_read_model : Specification
{
    ReadModelScenario<Thing4081> _scenario;

    void Establish() => _scenario = new ReadModelScenario<Thing4081>();

    async Task Because() => await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new Thing4081Configured(new("https://example.test"), "Thing"));

    [Fact] void should_populate_the_nullable_endpoint() => _scenario.Instance!.Endpoint.ShouldEqual(new Endpoint4081("https://example.test"));
    [Fact] void should_populate_the_name() => _scenario.Instance!.Name.ShouldEqual("Thing");
    [Fact] void should_report_no_substituted_layers() => _scenario.Substitutions.ShouldBeEmpty();
}
