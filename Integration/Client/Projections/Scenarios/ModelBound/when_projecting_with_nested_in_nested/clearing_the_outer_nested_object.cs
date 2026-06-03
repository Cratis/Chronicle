// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.Events;
using Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.ReadModels;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_with_nested_in_nested.clearing_the_outer_nested_object.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_with_nested_in_nested;

[Collection(ChronicleCollection.Name)]
public class clearing_the_outer_nested_object(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
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
            SliceId = Guid.Parse("b0e8396f-e8bc-c2a5-f391-805f5f3c9e48");

            var projectionId = EventStore.Projections.GetProjectionIdForModel<RecursiveSlice>();
            var handler = EventStore.Projections.GetAllHandlers().Single(_ => _.Id == projectionId);
            await handler.WaitTillSubscribed();

            await EventStore.EventLog.Append(SliceId, new RecursiveSliceItemCreated("Model-Bound Slice"));
            await EventStore.EventLog.Append(SliceId, new RecursiveCommandSetOnSliceItem("Register"));
            await EventStore.EventLog.Append(SliceId, new RecursiveValidationConfiguredOnCommand("must-not-be-empty"));
            var appendResult = await EventStore.EventLog.Append(SliceId, new RecursiveCommandClearedFromSliceItem());

            await handler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            Result = await EventStore.ReadModels.GetInstanceById<RecursiveSlice>(SliceId.ToString());
        }
    }

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_preserve_the_slice_name() => Context.Result.Name.ShouldEqual("Model-Bound Slice");
    [Fact] void should_have_cleared_the_nested_command() => Context.Result.Command.ShouldBeNull();
}
