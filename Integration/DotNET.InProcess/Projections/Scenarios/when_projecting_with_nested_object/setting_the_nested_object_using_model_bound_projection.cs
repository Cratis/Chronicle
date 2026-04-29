// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using MongoDB.Driver;
using context = Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_nested_object.setting_the_nested_object_using_model_bound_projection.context;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_nested_object;

[Collection(ChronicleCollection.Name)]
public class setting_the_nested_object_using_model_bound_projection(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture fixture) : Specification(fixture)
    {
        public Guid SliceId;
        public ModelBoundSlice Result;

        public override IEnumerable<Type> EventTypes => [typeof(ModelBoundSliceCreated), typeof(ModelBoundCommandSetForSlice), typeof(ModelBoundCommandCleared)];
        public override IEnumerable<Type> ModelBoundProjections => [typeof(ModelBoundSlice), typeof(ModelBoundCommandItem)];

        async Task Because()
        {
            SliceId = Guid.Parse("a3f1c208-d145-4b3e-8c2a-1f9e8bd52c71");

            var projectionId = EventStore.Projections.GetProjectionIdForModel<ModelBoundSlice>();
            var handler = EventStore.Projections.GetAllHandlers().Single(_ => _.Id == projectionId);
            await handler.WaitTillActive();

            await EventStore.EventLog.Append(SliceId, new ModelBoundSliceCreated("Model-Bound Slice"));
            var appendResult = await EventStore.EventLog.Append(SliceId, new ModelBoundCommandSetForSlice("Register", "{}"));

            await handler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            var collection = ChronicleFixture.ReadModels.Database.GetCollection<ModelBoundSlice>();
            Result = await (await collection.FindAsync(_ => true)).FirstOrDefaultAsync();
        }
    }

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_set_the_slice_name() => Context.Result.Name.ShouldEqual("Model-Bound Slice");
    [Fact] void should_have_a_nested_command() => Context.Result.Command.ShouldNotBeNull();
    [Fact] void should_set_the_command_name() => Context.Result.Command!.Name.ShouldEqual("Register");
    [Fact] void should_set_the_command_schema() => Context.Result.Command!.Schema.ShouldEqual("{}");
}

[EventType]
public record ModelBoundSliceCreated(string Name);

[EventType]
public record ModelBoundCommandSetForSlice(string Name, string Schema);

[EventType]
public record ModelBoundCommandCleared;

[FromEvent<ModelBoundCommandSetForSlice>]
[ClearWith<ModelBoundCommandCleared>]
public record ModelBoundCommandItem(string Name, string Schema);

[FromEvent<ModelBoundSliceCreated>]
public record ModelBoundSlice(
    Guid Id,
    string Name,
    [Nested] ModelBoundCommandItem? Command);

#pragma warning restore SA1402 // File may only contain a single type
