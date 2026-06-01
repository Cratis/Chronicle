// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;
using context = Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_nested_in_children.clearing_the_nested_object.context;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_nested_in_children;

[Collection(ChronicleCollection.Name)]
public class clearing_the_nested_object(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture fixture) : given.a_projection_and_events_appended_to_it<FeatureProjection, FeatureReadModel>(fixture)
    {
        public override IEnumerable<Type> EventTypes =>
        [
            typeof(FeatureCreated),
            typeof(SliceAddedToFeature),
            typeof(CommandSetOnSlice),
            typeof(CommandRenamedOnSlice),
            typeof(CommandClearedFromSlice)
        ];

        void Establish()
        {
            var featureId = Guid.Parse("e5f6a7b8-c9d0-1234-ef01-345678901234");
            var sliceId = Guid.Parse("f6a7b8c9-d0e1-2345-f012-456789012345");
            ReadModelId = featureId.ToString();

            EventsWithEventSourceIdToAppend.Add(new(featureId.ToString(), new FeatureCreated("My Feature")));
            EventsWithEventSourceIdToAppend.Add(new(sliceId.ToString(), new SliceAddedToFeature(featureId, sliceId, "My Slice")));
            EventsWithEventSourceIdToAppend.Add(new(sliceId.ToString(), new CommandSetOnSlice("Register", "{}")));
            EventsWithEventSourceIdToAppend.Add(new(sliceId.ToString(), new CommandClearedFromSlice()));
        }

        protected override async Task<FeatureReadModel> GetReadModelResult()
        {
            var collection = ChronicleInProcessFixture.ReadModels.Database.GetCollection<FeatureReadModel>();
            return await (await collection.FindAsync(_ => true)).FirstOrDefaultAsync();
        }
    }

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_have_one_slice() => Context.Result.Slices.Count().ShouldEqual(1);
    [Fact] void should_preserve_the_slice_name() => Context.Result.Slices.First().Name.ShouldEqual("My Slice");
    [Fact] void should_have_cleared_the_nested_command() => Context.Result.Slices.First().Command.ShouldBeNull();
    [Fact] void should_set_the_last_handled_event_sequence_number() => Context.Result.__lastHandledEventSequenceNumber.ShouldEqual(Context.LastEventSequenceNumber);
}
