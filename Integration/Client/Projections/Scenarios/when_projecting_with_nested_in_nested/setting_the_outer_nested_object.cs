// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_nested_in_nested.setting_the_outer_nested_object.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_nested_in_nested;

[Collection(ChronicleCollection.Name)]
public class setting_the_outer_nested_object(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_projection_and_events_appended_to_it<SliceProjection, DeepNestedSlice>(fixture)
    {
        public override IEnumerable<Type> EventTypes =>
        [
            typeof(DeepNestedSliceCreated),
            typeof(DeepNestedCommandSet),
            typeof(DeepNestedValidationConfigured),
            typeof(DeepNestedValidationUpdated),
            typeof(DeepNestedValidationRemoved),
            typeof(DeepNestedCommandCleared)
        ];

        void Establish()
        {
            EventsToAppend.Add(new DeepNestedSliceCreated("My Slice"));
            EventsToAppend.Add(new DeepNestedCommandSet("Register"));
        }
    }

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_set_the_slice_name() => Context.Result.Name.ShouldEqual("My Slice");
    [Fact] void should_have_a_nested_command() => Context.Result.Command.ShouldNotBeNull();
    [Fact] void should_set_the_command_name() => Context.Result.Command.Name.ShouldEqual("Register");
    [Fact] void should_not_have_a_nested_validation() => Context.Result.Command.Validation.ShouldBeNull();
    [Fact] void should_set_the_last_handled_event_sequence_number() => Context.Result.__lastHandledEventSequenceNumber.ShouldEqual(Context.LastEventSequenceNumber);
}

[EventType]
public record DeepNestedSliceCreated(string Name);

[EventType]
public record DeepNestedCommandSet(string Name);

[EventType]
public record DeepNestedValidationConfigured(string Rules);

[EventType]
public record DeepNestedValidationUpdated(string NewRules);

[EventType]
public record DeepNestedValidationRemoved;

[EventType]
public record DeepNestedCommandCleared;

public class DeepNestedValidationItem
{
    public string Rules { get; set; } = string.Empty;
}

public class DeepNestedCommandItem
{
    public string Name { get; set; } = string.Empty;
    public DeepNestedValidationItem? Validation { get; set; }
}

public record DeepNestedSlice(
    string Name,
    DeepNestedCommandItem? Command,
    EventSequenceNumber __lastHandledEventSequenceNumber = default!);

#pragma warning restore SA1402 // File may only contain a single type
