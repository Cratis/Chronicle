// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.InProcess.Integration.Projections.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.ReadModels;
using Cratis.Chronicle.InProcess.Integration.Projections.ProjectionTypes;
using context = Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_properties.using_key_from_property.context;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_properties;

[Collection(ChronicleCollection.Name)]
public class using_key_from_property(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : given.a_projection_and_events_appended_to_it<KeyFromPropertyProjection, ReadModel>(chronicleInProcessFixture)
    {
        public override IEnumerable<Type> EventTypes => [typeof(EventWithPropertiesForAllSupportedTypes)];

        void Establish()
        {
            ModelId = Guid.NewGuid().ToString();
            EventsToAppend.Add(EventWithPropertiesForAllSupportedTypes.CreateWithRandomValues() with
            {
                StringValue = ModelId
            });
        }
    }

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_set_the_event_sequence_number_to_last_event() => Context.Result.__lastHandledEventSequenceNumber.ShouldEqual(Context.LastEventSequenceNumber);
}
