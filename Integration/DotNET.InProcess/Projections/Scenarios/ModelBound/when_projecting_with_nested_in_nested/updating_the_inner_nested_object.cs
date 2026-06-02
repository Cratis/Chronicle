// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ModelBound.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ModelBound.ReadModels;
using MongoDB.Driver;
using context = Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ModelBound.when_projecting_with_nested_in_nested.updating_the_inner_nested_object.context;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ModelBound.when_projecting_with_nested_in_nested;

[Collection(ChronicleCollection.Name)]
public class updating_the_inner_nested_object(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture fixture) : Specification(fixture)
    {
        public Guid SliceId;
        public RecursiveSlice Result;

        public override IEnumerable<Type> EventTypes =>
        [
            typeof(RecursiveSliceItemCreated),
            typeof(RecursiveCommandSetOnSliceItem),
            typeof(RecursiveValidationConfiguredOnCommand),
            typeof(RecursiveValidationUpdatedOnCommand),
            typeof(RecursiveValidationRemovedFromCommand),
            typeof(RecursiveCommandClearedFromSliceItem)
        ];

        public override IEnumerable<Type> ModelBoundProjections =>
            [typeof(RecursiveSlice), typeof(RecursiveCommandItem), typeof(RecursiveValidationItem)];

        async Task Because()
        {
            SliceId = Guid.Parse("f8c6174d-c69a-a083-d17f-6e4d3f1a7c26");

            var projectionId = EventStore.Projections.GetProjectionIdForModel<RecursiveSlice>();
            var handler = EventStore.Projections.GetAllHandlers().Single(_ => _.Id == projectionId);
            await handler.WaitTillActive();

            await EventStore.EventLog.Append(SliceId, new RecursiveSliceItemCreated("Model-Bound Slice"));
            await EventStore.EventLog.Append(SliceId, new RecursiveCommandSetOnSliceItem("Register"));
            await EventStore.EventLog.Append(SliceId, new RecursiveValidationConfiguredOnCommand("must-not-be-empty"));
            var appendResult = await EventStore.EventLog.Append(SliceId, new RecursiveValidationUpdatedOnCommand("must-be-unique"));

            await handler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            var collection = ChronicleFixture.ReadModels.Database.GetCollection<RecursiveSlice>();
            Result = await (await collection.FindAsync(_ => true)).FirstOrDefaultAsync();
        }
    }

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_preserve_the_slice_name() => Context.Result.Name.ShouldEqual("Model-Bound Slice");
    [Fact] void should_have_a_nested_command() => Context.Result.Command.ShouldNotBeNull();
    [Fact] void should_preserve_the_command_name() => Context.Result.Command!.Name.ShouldEqual("Register");
    [Fact] void should_have_a_nested_validation() => Context.Result.Command!.Validation.ShouldNotBeNull();
    [Fact] void should_update_the_validation_rules() => Context.Result.Command!.Validation!.Rules.ShouldEqual("must-be-unique");
}
