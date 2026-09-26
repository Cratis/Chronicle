// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_nested_in_nested.recreating_the_inner_nested_object.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_nested_in_nested;

[Collection(ChronicleCollection.Name)]
public class recreating_the_inner_nested_object(context context) : Given<context>(context)
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
            EventsToAppend.Add(new DeepNestedValidationConfigured("old-rule"));
            EventsToAppend.Add(new DeepNestedValidationRemoved());
            EventsToAppend.Add(new DeepNestedValidationConfigured("new-rule"));
        }
    }

    [Fact] void should_preserve_the_outer_nested_object() => Context.Result.Command.Name.ShouldEqual("Register");
    [Fact] void should_recreate_the_inner_nested_object() => Context.Result.Command.Validation.Rules.ShouldEqual("new-rule");
    [Fact] void should_advance_the_watermark() => Context.Result.__lastHandledEventSequenceNumber.ShouldEqual(Context.LastEventSequenceNumber);
}
