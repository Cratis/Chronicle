// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.Events;
using Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.ReadModels;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_with_nested_in_children.clearing_the_nested_object.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_with_nested_in_children;

[Collection(ChronicleCollection.Name)]
public class clearing_the_nested_object(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public Guid FeatureId;
        public Guid SliceId;
        public FeatureReadModel Result;

        public override IEnumerable<Type> EventTypes =>
        [
            typeof(FeatureCreated),
            typeof(SliceAddedToFeature),
            typeof(CommandSetOnSlice),
            typeof(CommandRenamedOnSlice),
            typeof(CommandClearedFromSlice),
            typeof(EventAddedToSlice)
        ];

        public override IEnumerable<Type> ModelBoundProjections => [typeof(FeatureReadModel), typeof(SliceItem), typeof(SliceCommandItem)];

        async Task Because()
        {
            FeatureId = Guid.Parse("e5f6a7b8-c9d0-1234-ef01-345678901234");
            SliceId = Guid.Parse("f6a7b8c9-d0e1-2345-f012-456789012345");

            var projectionId = EventStore.Projections.GetProjectionIdForModel<FeatureReadModel>();
            var handler = EventStore.Projections.GetAllHandlers().Single(_ => _.Id == projectionId);
            await handler.WaitTillActive();

            await EventStore.EventLog.Append(FeatureId, new FeatureCreated("My Feature"));
            await EventStore.EventLog.Append(SliceId, new SliceAddedToFeature(FeatureId, SliceId, "My Slice"));

            var sliceAppendResult = await EventStore.EventLog.Append(SliceId, new CommandSetOnSlice("Register", "{}"));
            await handler.WaitTillReachesEventSequenceNumber(sliceAppendResult.SequenceNumber);

            await EventStore.EventLog.Append(SliceId, new CommandClearedFromSlice());

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (!cts.IsCancellationRequested)
            {
                Result = await EventStore.ReadModels.GetInstanceById<FeatureReadModel>(FeatureId.ToString());
                if (Result?.Slices?.FirstOrDefault()?.Command is null)
                {
                    break;
                }

                await Task.Delay(50);
            }

            Result = await EventStore.ReadModels.GetInstanceById<FeatureReadModel>(FeatureId.ToString());
        }
    }

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_have_one_slice() => Context.Result.Slices.Count().ShouldEqual(1);
    [Fact] void should_preserve_the_slice_name() => Context.Result.Slices.First().Name.ShouldEqual("My Slice");
    [Fact] void should_have_cleared_the_nested_command() => Context.Result.Slices.First().Command.ShouldBeNull();
}
