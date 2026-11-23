// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.Events;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_deep_self_referential_children.given;

public class a_recursive_module(ChronicleFixture fixture) : Specification(fixture)
{
    public Guid ModuleId = Guid.NewGuid();
    public Guid RootId = Guid.NewGuid();
    public Guid FirstId = Guid.NewGuid();
    public Guid SecondId = Guid.NewGuid();
    public Guid AddedId = Guid.NewGuid();
    public RecursiveModule Result;

    IProjectionHandler _handler;

    public override IEnumerable<Type> EventTypes => [typeof(ModuleAdded), typeof(FeatureAddedToModule), typeof(SubFeatureAdded), typeof(SubFeatureRemoved), typeof(SliceAddedToFeature)];
    public override IEnumerable<Type> ModelBoundProjections => [typeof(RecursiveModule), typeof(RecursiveFeature)];

    async Task Establish()
    {
        var projectionId = EventStore.Projections.GetProjectionIdForModel<RecursiveModule>();
        _handler = EventStore.Projections.GetAllHandlers().Single(_ => _.Id == projectionId);
        await _handler.WaitTillSubscribed();
        await Append(ModuleId, new ModuleAdded("Module"));
        await Append(RootId, new FeatureAddedToModule(ModuleId, RootId, "Root"));
        await Append(FirstId, new SubFeatureAdded(RootId, "First"));
        await Append(SecondId, new SubFeatureAdded(FirstId, "Second"));
        await ReadResult();
    }

    protected async Task Append(Guid source, object @event)
    {
        var result = await EventStore.EventLog.Append(source, @event);
        await _handler.WaitTillReachesEventSequenceNumber(result.SequenceNumber);
    }

    protected async Task ReadResult() => Result = await EventStore.ReadModels.GetInstanceById<RecursiveModule>(ModuleId.ToString());
}
