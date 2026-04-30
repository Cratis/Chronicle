// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ModelBound.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ModelBound.ReadModels;
using MongoDB.Driver;
using context = Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ModelBound.when_projecting_with_nested_object.setting_the_nested_object.context;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ModelBound.when_projecting_with_nested_object;

[Collection(ChronicleCollection.Name)]
public class setting_the_nested_object(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture fixture) : Specification(fixture)
    {
        public Guid SliceId;
        public Slice Result;

        public override IEnumerable<Type> EventTypes =>
            [typeof(SliceCreated), typeof(CommandSetForSlice), typeof(SliceCommandRenamed), typeof(CommandClearedForSlice)];

        public override IEnumerable<Type> ModelBoundProjections => [typeof(Slice), typeof(CommandItem)];

        async Task Because()
        {
            SliceId = Guid.Parse("a3f1c208-d145-4b3e-8c2a-1f9e8bd52c71");

            var projectionId = EventStore.Projections.GetProjectionIdForModel<Slice>();
            var handler = EventStore.Projections.GetAllHandlers().Single(_ => _.Id == projectionId);
            await handler.WaitTillActive();

            await EventStore.EventLog.Append(SliceId, new SliceCreated("Model-Bound Slice"));
            var appendResult = await EventStore.EventLog.Append(SliceId, new CommandSetForSlice("Register", "{}"));

            await handler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            var collection = ChronicleFixture.ReadModels.Database.GetCollection<Slice>();
            Result = await (await collection.FindAsync(_ => true)).FirstOrDefaultAsync();
        }
    }

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_set_the_slice_name() => Context.Result.Name.ShouldEqual("Model-Bound Slice");
    [Fact] void should_have_a_nested_command() => Context.Result.Command.ShouldNotBeNull();
    [Fact] void should_set_the_command_name() => Context.Result.Command!.Name.ShouldEqual("Register");
    [Fact] void should_set_the_command_schema() => Context.Result.Command!.Schema.ShouldEqual("{}");
}
