// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Projections;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads;

public class when_comparing_kernel_definitions : given.a_decision_reader
{
    static readonly Dictionary<string, Action<ProjectionDefinition>> _changes = new()
    {
        ["ReadModel"] = _ => _.ReadModel = "other",
        ["EventSequenceId"] = _ => _.EventSequenceId = "other",
        ["GenerationDelivery"] = _ => _.GenerationDelivery = EventGenerationDelivery.Pinned,
        ["SubscribesToAllEvents"] = _ => _.SubscribesToAllEvents = true,
        ["Join"] = _ => _.Join = new Dictionary<Contracts.Events.EventType, JoinDefinition> { [new() { Id = "joined" }] = new() },
        ["RemovedWithJoin"] = _ => _.RemovedWithJoin = new Dictionary<Contracts.Events.EventType, RemovedWithJoinDefinition> { [new() { Id = "removed" }] = new() },
        ["Children"] = _ => _.Children = new Dictionary<string, ChildrenDefinition> { ["child"] = new() },
        ["Nested"] = _ => _.Nested = new Dictionary<string, ChildrenDefinition> { ["nested"] = new() },
        ["FromEvery"] = _ => _.FromEvery = [new()],
        ["FromEventProperty"] = _ => _.FromEventProperty = new(),
        ["EventTypes"] = _ => _.From = new Dictionary<Contracts.Events.EventType, FromDefinition> { [new() { Id = "other" }] = new() },
        ["All.Key"] = _ => _.All = new() { Key = "other" },
        ["From.Key"] = _ => _.From = new Dictionary<Contracts.Events.EventType, FromDefinition> { [new() { Id = "created", Generation = 1 }] = new() { Key = "other" } },
        ["From.ParentKey"] = _ => _.From = new Dictionary<Contracts.Events.EventType, FromDefinition> { [new() { Id = "created", Generation = 1 }] = new() { ParentKey = "other" } },
        ["RemovedWith event types or key routing"] = _ => _.RemovedWith = new Dictionary<Contracts.Events.EventType, RemovedWithDefinition> { [new() { Id = "removed" }] = new() }
    };

    public static TheoryData<string> Differences => [.. _changes.Keys];

    [Theory]
    [MemberData(nameof(Differences))]
    public async Task should_name_the_differing_part_and_refuse_before_folding(string difference)
    {
        var kernel = new ProjectionDefinition
        {
            Identifier = _definition.Identifier,
            ReadModel = _definition.ReadModel,
            EventSequenceId = _definition.EventSequenceId,
            From = _definition.From
        };
        _changes[difference](kernel);
        _projectionService.GetAllDefinitions(Arg.Any<GetAllDefinitionsRequest>(), Arg.Any<CallContext>()).Returns([kernel]);
        var error = await Catch.Exception(() => _reader.GetDetached<Model>("source"));

        ((DecisionReadRefused)error).Reason.ShouldEqual(DecisionReadRefusalReason.DefinitionMismatch);
        error.Message.ShouldContain(difference.StartsWith("From.", StringComparison.Ordinal) ? "From event types or key routing" : difference);
        _folds.ShouldEqual(0);
        _projectionService.ReceivedCalls().Count().ShouldEqual(2);
    }
}
