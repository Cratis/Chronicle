// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Events;
using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Models;
using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.ProjectionTypes;
using MongoDB.Driver;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.when_projecting_properties.using_composite_key_from_property_and_context_property.context;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.when_projecting_properties;

[Collection(ChronicleCollection.Name)]
public class using_composite_key_from_property_and_context_property(context context) : Given<context>(context)
{
    public class context(ChronicleMongoDBFixture chronicleMongoDbFixture) : given.a_projection_and_events_appended_to_it<CompositeKeyFromPropertyAndContextPropertyProjection, ModelWithCompositeKey>(chronicleMongoDbFixture)
    {
        public override IEnumerable<Type> EventTypes => [typeof(EventWithPropertiesForAllSupportedTypes)];
        public CompositeKey CompositeId;
        public ModelWithCompositeKey Model;

        void Establish()
        {
            CompositeId = new(Guid.NewGuid().ToString(), 0);
            EventsToAppend.Add(EventWithPropertiesForAllSupportedTypes.CreateWithRandomValues() with
            {
                StringValue = CompositeId.First
            });
        }

        async Task Because()
        {
            var result = await chronicleMongoDbFixture.ReadModels.Database.GetCollection<ModelWithCompositeKey>().FindAsync(_ => _.Id == CompositeId);
            Model = await result.FirstOrDefaultAsync();
        }
    }

    [Fact] void should_return_model() => Context.Model.ShouldNotBeNull();
    [Fact] void should_set_the_event_sequence_number_to_last_event() => Context.Model.__lastHandledEventSequenceNumber.ShouldEqual(Context.AppendedEvents[^1].Metadata.SequenceNumber);
}
