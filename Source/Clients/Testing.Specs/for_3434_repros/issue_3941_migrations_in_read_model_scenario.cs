// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402, SA1649

using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Chronicle.Testing.ReadModels;

namespace Cratis.Chronicle.Testing.for_3434_repros.issue_3941;

/// <summary>
/// A module key concept.
/// </summary>
/// <param name="Value">The value.</param>
public record Module3941(string Value) : ConceptAs<string>(Value);

/// <summary>
/// Current generation: a topic created in a module.
/// </summary>
/// <param name="Module">The module.</param>
[EventType(generation: 2)]
public record Topic3941Created(Module3941 Module);

/// <summary>
/// First generation: a topic created without a module.
/// </summary>
[EventTypeGenerationFor<Topic3941Created>(1)]
public record Topic3941CreatedV1;

/// <summary>
/// Migration defaulting the module, using the primitive-typed workaround.
/// </summary>
public class Topic3941CreatedV1ToV2 : EventTypeMigration<Topic3941Created, Topic3941CreatedV1>
{
    /// <inheritdoc/>
    public override void Upcast(IEventMigrationBuilder<Topic3941Created, Topic3941CreatedV1> builder) =>
        builder.Properties(p => p.DefaultValue<string>(_ => _.Module, "global"));

    /// <inheritdoc/>
    public override void Downcast(IEventMigrationBuilder<Topic3941CreatedV1, Topic3941Created> builder) =>
        builder.Properties(_ => { });
}

/// <summary>
/// Current generation: an area assigned to a module (concept-typed default value).
/// </summary>
/// <param name="Module">The module.</param>
[EventType(generation: 2)]
public record Area3941Assigned(Module3941 Module);

/// <summary>
/// First generation of <see cref="Area3941Assigned"/>.
/// </summary>
[EventTypeGenerationFor<Area3941Assigned>(1)]
public record Area3941AssignedV1;

/// <summary>
/// Migration defaulting the module, typed as the concept (the documented call shape).
/// </summary>
public class Area3941AssignedV1ToV2 : EventTypeMigration<Area3941Assigned, Area3941AssignedV1>
{
    /// <inheritdoc/>
    public override void Upcast(IEventMigrationBuilder<Area3941Assigned, Area3941AssignedV1> builder) =>
        builder.Properties(p => p.DefaultValue<Module3941>(_ => _.Module, new Module3941("global")));

    /// <inheritdoc/>
    public override void Downcast(IEventMigrationBuilder<Area3941AssignedV1, Area3941Assigned> builder) =>
        builder.Properties(_ => { });
}

/// <summary>
/// Read model built from the current generation.
/// </summary>
/// <param name="Id">The id.</param>
/// <param name="Module">The module.</param>
[FromEvent<Topic3941Created>]
public record Topic3941(Guid Id, Module3941 Module);

public class when_seeding_an_old_generation_event_into_read_model_scenario : Specification
{
    ReadModelScenario<Topic3941> _scenario;

    void Establish() => _scenario = new ReadModelScenario<Topic3941>();

    async Task Because() => await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new Topic3941CreatedV1());

    [Fact] void should_materialize_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_upcast_the_module_default() => _scenario.Instance!.Module.ShouldEqual(new Module3941("global"));
}

public class when_upcasting_with_a_concept_typed_default_value : Specification
{
    EventMigrationBuilder _builder;
    JsonObject _result;

    void Establish() => _builder = new EventMigrationBuilder(null);

    void Because()
    {
        ((IEventTypeMigration)new Area3941AssignedV1ToV2()).Upcast(_builder);
        _result = _builder.ToJson();
    }

    [Fact] void should_emit_the_raw_primitive_default() => _result["Module"]!["$defaultValue"]!.ToJsonString().ShouldEqual("\"global\"");
}

public class when_upcasting_with_a_primitive_typed_default_value : Specification
{
    EventMigrationBuilder _builder;
    JsonObject _result;

    void Establish() => _builder = new EventMigrationBuilder(null);

    void Because()
    {
        ((IEventTypeMigration)new Topic3941CreatedV1ToV2()).Upcast(_builder);
        _result = _builder.ToJson();
    }

    [Fact] void should_emit_the_raw_primitive_default() => _result["Module"]!["$defaultValue"]!.ToJsonString().ShouldEqual("\"global\"");
}

public class when_appending_an_old_generation_event_through_event_scenario : Specification, IDisposable
{
    EventScenario _scenario;
    IImmutableList<AppendedEvent> _events;
    AppendResult _result;

    void Establish() => _scenario = new EventScenario();

    async Task Because()
    {
        _result = await _scenario.EventLog.Append(EventSourceId.New(), new Topic3941CreatedV1());
        _events = await _scenario.EventSequence.GetFromSequenceNumber(EventSequenceNumber.First);
    }

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] void should_read_back_the_current_generation_with_the_default() => ((Topic3941Created)_events[0].Content).Module.ShouldEqual(new Module3941("global"));

    public void Dispose() => _scenario.Dispose();
}
