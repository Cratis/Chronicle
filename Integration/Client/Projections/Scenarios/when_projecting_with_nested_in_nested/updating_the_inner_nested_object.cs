// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_nested_in_nested.updating_the_inner_nested_object.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_nested_in_nested;

[Collection(ChronicleCollection.Name)]
public class updating_the_inner_nested_object(context context) : Given<context>(context)
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
            EventsToAppend.Add(new DeepNestedValidationConfigured("must-not-be-empty"));
            EventsToAppend.Add(new DeepNestedValidationUpdated("must-be-unique"));
        }
    }

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_preserve_the_slice_name() => Context.Result.Name.ShouldEqual("My Slice");
    [Fact] void should_have_a_nested_command() => Context.Result.Command.ShouldNotBeNull();
    [Fact] void should_preserve_the_command_name() => Context.Result.Command.Name.ShouldEqual("Register");
    [Fact] void should_have_a_nested_validation() => Context.Result.Command.Validation.ShouldNotBeNull();
    [Fact] void should_update_the_validation_rules() => Context.Result.Command.Validation.Rules.ShouldEqual("must-be-unique");
    [Fact] void should_set_the_last_handled_event_sequence_number() => Context.Result.__lastHandledEventSequenceNumber.ShouldEqual(Context.LastEventSequenceNumber);
}
