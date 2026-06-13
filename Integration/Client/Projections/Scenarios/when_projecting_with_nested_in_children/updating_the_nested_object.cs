// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_nested_in_children.updating_the_nested_object.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_nested_in_children;

[Collection(ChronicleCollection.Name)]
public class updating_the_nested_object(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_projection_and_events_appended_to_it<FeatureProjection, NestedFeatureReadModel>(fixture)
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
            var featureId = Guid.Parse("c3d4e5f6-a7b8-9012-cdef-123456789012");
            var sliceId = Guid.Parse("d4e5f6a7-b8c9-0123-def0-234567890123");
            ReadModelId = featureId.ToString();

            EventsWithEventSourceIdToAppend.Add(new(featureId.ToString(), new FeatureCreated("My Feature")));
            EventsWithEventSourceIdToAppend.Add(new(sliceId.ToString(), new SliceAddedToFeature(featureId, sliceId, "My Slice")));
            EventsWithEventSourceIdToAppend.Add(new(sliceId.ToString(), new CommandSetOnSlice("Register", "{}")));
            EventsWithEventSourceIdToAppend.Add(new(sliceId.ToString(), new CommandRenamedOnSlice("Create")));
        }
    }

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_have_one_slice() => Context.Result.Slices.Count().ShouldEqual(1);
    [Fact] void should_have_a_nested_command_on_the_slice() => Context.Result.Slices.First().Command.ShouldNotBeNull();
    [Fact] void should_update_the_command_name() => Context.Result.Slices.First().Command.Name.ShouldEqual("Create");
    [Fact] void should_preserve_the_command_schema() => Context.Result.Slices.First().Command.Schema.ShouldEqual("{}");
    [Fact] void should_set_the_last_handled_event_sequence_number() => Context.Result.__lastHandledEventSequenceNumber.ShouldEqual(Context.LastEventSequenceNumber);
}
