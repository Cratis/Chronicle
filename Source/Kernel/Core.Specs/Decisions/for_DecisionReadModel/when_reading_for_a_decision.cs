// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Setup.Serialization;

namespace Cratis.Chronicle.Decisions.for_DecisionReadModel;

public class when_reading_for_a_decision
{
    readonly EventType _created = new("Created", EventTypeGeneration.First);
    readonly EventType _removed = new("Removed", EventTypeGeneration.First);
    readonly IReadModel _readModel = Substitute.For<IReadModel>();
    readonly IProjection _projection = Substitute.For<IProjection>();
    readonly IImmediateProjection _immediate = Substitute.For<IImmediateProjection>();
    readonly IGrainFactory _grains = Substitute.For<IGrainFactory>();
    readonly IReadModelsCompliance _compliance = Substitute.For<IReadModelsCompliance>();

    public when_reading_for_a_decision()
    {
        var definition = new ReadModelDefinition(
            "model",
            "model",
            "Model",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            "projection",
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, new JsonSchema() } },
            []);
        _readModel.GetDefinition().Returns(definition);
        _grains.GetGrain<IReadModel>(Arg.Any<string>()).Returns(_readModel);
        _grains.GetGrain<IProjection>(Arg.Any<string>()).Returns(_projection);
        _grains.GetGrain<IImmediateProjection>(Arg.Any<string>()).Returns(_immediate);
        _projection.GetDefinition().Returns(new ProjectionDefinition(
            Concepts.Projections.ProjectionOwner.Client,
            EventSequenceId.Log,
            "projection",
            "model",
            true,
            true,
            new JsonObject(),
            new Dictionary<EventType, FromDefinition>(),
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>()));
        _projection.GetDecisionProjectionShape(Arg.Any<EventStoreNamespaceName>())
            .Returns(new DecisionProjectionShape(true, false, false, false, [_created, _removed]));
        _immediate.GetModelInstance().Returns(new ProjectionResult(new JsonObject { ["name"] = "Created" }, 1, 7));
    }

    Task<DecisionReadModel> Read() => DecisionReadModel.GetInstanceForDecision(
        "store", "namespace", "model", "source", _grains, _compliance, CreateOptions());

    static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        SerializationConfigurationExtensions.ApplyConverters(options);
        return options;
    }

    [Fact]
    public async Task should_return_the_present_instance_and_both_creation_and_removal_types()
    {
        var read = await Read();
        read.Refusal.ShouldEqual(DecisionReadRefusal.None);
        read.Instance.ShouldContain("Created");
        read.SequenceNumber.ShouldEqual(7UL);
        read.EventTypes.ShouldContain(_created.ToString());
        read.EventTypes.ShouldContain(_removed.ToString());
        await _immediate.Received(1).InitializeForDecision(
            Arg.Any<ProjectionDefinition>(),
            Arg.Is<IEnumerable<EventType>>(types => types.Contains(_created) && types.Contains(_removed) && types.Count() == 2));
    }

    [Fact]
    public async Task should_report_never_created_with_no_matching_watermark()
    {
        _immediate.GetModelInstance().Returns(ProjectionResult.Empty);
        var read = await Read();
        read.Instance.ShouldEqual("null");
        read.SequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable.Value);
    }

    [Fact]
    public async Task should_report_removal_with_its_exact_watermark()
    {
        _immediate.GetModelInstance().Returns(ProjectionResult.Empty with { LastHandledEventSequenceNumber = 9 });
        var read = await Read();
        read.Instance.ShouldEqual("null");
        read.SequenceNumber.ShouldEqual(9UL);
    }

    [Fact]
    public async Task should_admit_an_unchanged_populated_definition_and_refuse_a_changed_one()
    {
        var definition = await _projection.GetDefinition();
        var populated = definition with
        {
            From = new Dictionary<EventType, FromDefinition>
            {
                [_created] = new(new Dictionary<PropertyPath, string> { ["name"] = "$name" }, WellKnownExpressions.EventSourceId, null)
            },
            RemovedWith = new Dictionary<EventType, RemovedWithDefinition>
            {
                [_removed] = new(WellKnownExpressions.EventSourceId, null)
            }
        };
        _projection.GetDefinition().Returns(populated, populated with { LastUpdated = DateTimeOffset.UtcNow });
        (await Read()).Refusal.ShouldEqual(DecisionReadRefusal.None);

        _projection.GetDefinition().Returns(populated, populated with
        {
            From = new Dictionary<EventType, FromDefinition>
            {
                [_created] = new(new Dictionary<PropertyPath, string> { ["name"] = "$changed" }, WellKnownExpressions.EventSourceId, null)
            }
        });
        (await Read()).Refusal.ShouldEqual(DecisionReadRefusal.DefinitionChanged);
    }

    [Fact]
    public async Task should_refuse_a_reducer()
    {
        var definition = await _readModel.GetDefinition();
        _readModel.GetDefinition().Returns(definition with { ObserverType = ReadModelObserverType.Reducer });
        (await Read()).Refusal.ShouldEqual(DecisionReadRefusal.Reducer);
    }

    [Fact]
    public async Task should_refuse_an_unknown_definition()
    {
        _readModel.GetDefinition().Returns(Task.FromResult<ReadModelDefinition>(null!));
        (await Read()).Refusal.ShouldEqual(DecisionReadRefusal.UnknownDefinition);
    }

    [Fact]
    public async Task should_refuse_an_unspecified_key()
    {
        var result = await DecisionReadModel.GetInstanceForDecision(
            "store", "namespace", "model", ReadModelKey.Unspecified, _grains, _compliance, new JsonSerializerOptions());
        result.Refusal.ShouldEqual(DecisionReadRefusal.UnspecifiedKey);
    }

    [Fact]
    public async Task should_refuse_a_non_log_projection()
    {
        var definition = await _projection.GetDefinition();
        _projection.GetDefinition().Returns(definition with { EventSequenceId = "custom-sequence" });
        (await Read()).Refusal.ShouldEqual(DecisionReadRefusal.NotEventLog);
    }

    [Theory]
    [InlineData(true, false, false, false, DecisionReadRefusal.Join)]
    [InlineData(false, true, false, false, DecisionReadRefusal.Hierarchy)]
    [InlineData(false, false, true, false, DecisionReadRefusal.OpenEndedEventTypes)]
    [InlineData(false, false, false, false, DecisionReadRefusal.NotEventSourceKeyed)]
    public async Task should_refuse_non_admitted_shapes(bool joins, bool children, bool allEvents, bool keyed, DecisionReadRefusal reason)
    {
        _projection.GetDecisionProjectionShape(Arg.Any<EventStoreNamespaceName>())
            .Returns(new DecisionProjectionShape(keyed, joins, children, allEvents, [_created, _removed]));
        (await Read()).Refusal.ShouldEqual(reason);
    }
}
