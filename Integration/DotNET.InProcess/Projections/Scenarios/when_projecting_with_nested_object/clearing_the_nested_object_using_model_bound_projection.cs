// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;
using context = Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_nested_object.clearing_the_nested_object_using_model_bound_projection.context;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_nested_object;

[Collection(ChronicleCollection.Name)]
public class clearing_the_nested_object_using_model_bound_projection(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture fixture) : Specification(fixture)
    {
        public Guid SliceId;
        public ModelBoundSlice Result;

        public override IEnumerable<Type> EventTypes => [typeof(ModelBoundSliceCreated), typeof(ModelBoundCommandSetForSlice), typeof(ModelBoundCommandCleared)];
        public override IEnumerable<Type> ModelBoundProjections => [typeof(ModelBoundSlice), typeof(ModelBoundCommandItem)];

        async Task Because()
        {
            SliceId = Guid.Parse("b4e2d319-e256-5c4f-9d3b-2g0f9ce63d82");

            var projectionId = EventStore.Projections.GetProjectionIdForModel<ModelBoundSlice>();
            var handler = EventStore.Projections.GetAllHandlers().Single(_ => _.Id == projectionId);
            await handler.WaitTillActive();

            await EventStore.EventLog.Append(SliceId, new ModelBoundSliceCreated("Model-Bound Slice"));
            await EventStore.EventLog.Append(SliceId, new ModelBoundCommandSetForSlice("Register", "{}"));
            var appendResult = await EventStore.EventLog.Append(SliceId, new ModelBoundCommandCleared());

            await handler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            var collection = ChronicleFixture.ReadModels.Database.GetCollection<ModelBoundSlice>();
            Result = await (await collection.FindAsync(_ => true)).FirstOrDefaultAsync();
        }
    }

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_preserve_the_slice_name() => Context.Result.Name.ShouldEqual("Model-Bound Slice");
    [Fact] void should_have_cleared_the_nested_command() => Context.Result.Command.ShouldBeNull();
}
