// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_nested_object.updating_the_nested_object.context;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_nested_object;

[Collection(ChronicleCollection.Name)]
public class updating_the_nested_object(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture fixture) : given.a_projection_and_events_appended_to_it<SliceProjection, Slice>(fixture)
    {
        public override IEnumerable<Type> EventTypes => [typeof(SliceCreated), typeof(CommandSetForSlice), typeof(SliceCommandRenamed), typeof(CommandClearedForSlice)];

        void Establish()
        {
            EventsToAppend.Add(new SliceCreated("My Slice"));
            EventsToAppend.Add(new CommandSetForSlice("Register", "{}"));
            EventsToAppend.Add(new SliceCommandRenamed("Create"));
        }
    }

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_have_a_nested_command() => Context.Result.Command.ShouldNotBeNull();
    [Fact] void should_update_the_command_name() => Context.Result.Command!.Name.ShouldEqual("Create");
    [Fact] void should_preserve_the_command_schema() => Context.Result.Command!.Schema.ShouldEqual("{}");
    [Fact] void should_set_the_last_handled_event_sequence_number() => Context.Result.__lastHandledEventSequenceNumber.ShouldEqual(Context.LastEventSequenceNumber);
}
