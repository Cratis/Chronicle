// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.Events;
using Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.ReadModels;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_with_nested_in_nested.setting_the_inner_nested_object.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_with_nested_in_nested;

[Collection(ChronicleCollection.Name)]
public class setting_the_inner_nested_object(context context) : Given<context>(context)
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
            SliceId = Guid.Parse("e7b5063c-b589-8f72-c06e-5d3c2f096b15");

            var projectionId = EventStore.Projections.GetProjectionIdForModel<RecursiveSlice>();
            var handler = EventStore.Projections.GetAllHandlers().Single(_ => _.Id == projectionId);
            await handler.WaitTillSubscribed();

            await EventStore.EventLog.Append(SliceId, new RecursiveSliceItemCreated("Model-Bound Slice"));
            await EventStore.EventLog.Append(SliceId, new RecursiveCommandSetOnSliceItem("Register"));
            var appendResult = await EventStore.EventLog.Append(SliceId, new RecursiveValidationConfiguredOnCommand("must-not-be-empty"));

            await handler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            Result = await EventStore.ReadModels.GetInstanceById<RecursiveSlice>(SliceId.ToString());
        }
    }

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_preserve_the_slice_name() => Context.Result.Name.ShouldEqual("Model-Bound Slice");
    [Fact] void should_have_a_nested_command() => Context.Result.Command.ShouldNotBeNull();
    [Fact] void should_preserve_the_command_name() => Context.Result.Command.Name.ShouldEqual("Register");
    [Fact] void should_have_a_nested_validation() => Context.Result.Command.Validation.ShouldNotBeNull();
    [Fact] void should_set_the_validation_rules() => Context.Result.Command.Validation.Rules.ShouldEqual("must-not-be-empty");
}
