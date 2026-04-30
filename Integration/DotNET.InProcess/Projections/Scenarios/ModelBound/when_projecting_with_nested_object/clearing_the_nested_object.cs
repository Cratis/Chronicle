// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ModelBound.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ModelBound.ReadModels;
using MongoDB.Driver;
using context = Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ModelBound.when_projecting_with_nested_object.clearing_the_nested_object.context;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ModelBound.when_projecting_with_nested_object;

[Collection(ChronicleCollection.Name)]
public class clearing_the_nested_object(context context) : Given<context>(context)
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
            SliceId = Guid.Parse("c5f3e42a-f367-6d50-ae4c-3b1a0df74e93");

            var projectionId = EventStore.Projections.GetProjectionIdForModel<Slice>();
            var handler = EventStore.Projections.GetAllHandlers().Single(_ => _.Id == projectionId);
            await handler.WaitTillActive();

            await EventStore.EventLog.Append(SliceId, new SliceCreated("Model-Bound Slice"));
            await EventStore.EventLog.Append(SliceId, new CommandSetForSlice("Register", "{}"));
            var appendResult = await EventStore.EventLog.Append(SliceId, new CommandClearedForSlice());

            await handler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            var collection = ChronicleFixture.ReadModels.Database.GetCollection<Slice>();
            Result = await (await collection.FindAsync(_ => true)).FirstOrDefaultAsync();
        }
    }

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_preserve_the_slice_name() => Context.Result.Name.ShouldEqual("Model-Bound Slice");
    [Fact] void should_have_cleared_the_nested_command() => Context.Result.Command.ShouldBeNull();
}
