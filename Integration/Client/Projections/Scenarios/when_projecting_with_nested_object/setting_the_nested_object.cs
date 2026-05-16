// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_nested_object.setting_the_nested_object.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_nested_object;

[Collection(ChronicleCollection.Name)]
public class setting_the_nested_object(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_projection_and_events_appended_to_it<SliceProjection, NestedSlice>(fixture)
    {
        public override IEnumerable<Type> EventTypes => [typeof(SliceCreated), typeof(CommandSetForSlice), typeof(SliceCommandRenamed), typeof(CommandClearedForSlice)];

        void Establish()
        {
            EventsToAppend.Add(new SliceCreated("My Slice"));
            EventsToAppend.Add(new CommandSetForSlice("Register", "{}"));
        }
    }

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_set_the_slice_name() => Context.Result.Name.ShouldEqual("My Slice");
    [Fact] void should_have_a_nested_command() => Context.Result.Command.ShouldNotBeNull();
    [Fact] void should_set_the_command_name() => Context.Result.Command!.Name.ShouldEqual("Register");
    [Fact] void should_set_the_command_schema() => Context.Result.Command!.Schema.ShouldEqual("{}");
    [Fact] void should_set_the_last_handled_event_sequence_number() => Context.Result.__lastHandledEventSequenceNumber.ShouldEqual(Context.LastEventSequenceNumber);
}

[EventType]
public record SliceCreated(string Name);

[EventType]
public record CommandSetForSlice(string Name, string Schema);

[EventType]
public record SliceCommandRenamed(string NewName);

[EventType]
public record CommandClearedForSlice;

public class NestedCommandItem
{
    public string Name { get; set; } = string.Empty;
    public string Schema { get; set; } = string.Empty;
}

public record NestedSlice(
    string Name,
    NestedCommandItem? Command,
    EventSequenceNumber __lastHandledEventSequenceNumber = default!);

#pragma warning restore SA1402 // File may only contain a single type
