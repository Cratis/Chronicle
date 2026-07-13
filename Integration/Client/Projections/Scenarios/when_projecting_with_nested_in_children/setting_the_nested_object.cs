// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_nested_in_children.setting_the_nested_object.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_nested_in_children;

[Collection(ChronicleCollection.Name)]
public class setting_the_nested_object(context context) : Given<context>(context)
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
            var featureId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
            var sliceId = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901");
            ReadModelId = featureId.ToString();

            EventsWithEventSourceIdToAppend.Add(new(featureId.ToString(), new FeatureCreated("My Feature")));
            EventsWithEventSourceIdToAppend.Add(new(sliceId.ToString(), new SliceAddedToFeature(featureId, sliceId, "My Slice")));
            EventsWithEventSourceIdToAppend.Add(new(sliceId.ToString(), new CommandSetOnSlice("Register", "{}")));
        }
    }

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_have_the_feature_name() => Context.Result.Name.ShouldEqual("My Feature");
    [Fact] void should_have_one_slice() => Context.Result.Slices.Count().ShouldEqual(1);
    [Fact] void should_have_a_nested_command_on_the_slice() => Context.Result.Slices.First().Command.ShouldNotBeNull();
    [Fact] void should_set_the_command_name() => Context.Result.Slices.First().Command.Name.ShouldEqual("Register");
    [Fact] void should_set_the_command_schema() => Context.Result.Slices.First().Command.Schema.ShouldEqual("{}");
    [Fact] void should_set_the_last_handled_event_sequence_number() => Context.Result.__lastHandledEventSequenceNumber.ShouldEqual(Context.LastEventSequenceNumber);
}

[EventType]
public record FeatureCreated(string Name);

[EventType]
public record SliceAddedToFeature(Guid FeatureId, Guid SliceId, string Name);

[EventType]
public record CommandSetOnSlice(string Name, string Schema);

[EventType]
public record CommandRenamedOnSlice(string NewName);

[EventType]
public record CommandClearedFromSlice;

public class SliceCommandItem
{
    public string Name { get; set; } = string.Empty;
    public string Schema { get; set; } = string.Empty;
}

public class SliceItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public SliceCommandItem? Command { get; set; }
}

public record NestedFeatureReadModel(
    string Name,
    IEnumerable<SliceItem> Slices,
    EventSequenceNumber __lastHandledEventSequenceNumber = default!);

#pragma warning restore SA1402 // File may only contain a single type
