// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.Base;
using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Events;
using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.ProjectionTypes;
using Cratis.Chronicle.Projections;
using HandlebarsDotNet;
using MongoDB.Driver;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.when_projecting_properties;

[Collection(GlobalCollection.Name)]
public class with_simple_set_operations(with_simple_set_operations.context fixture) : OrleansTest<with_simple_set_operations.context>(fixture)
{
    public class context(GlobalFixture globalFixture) : IntegrationTestSetup(globalFixture)
    {
        public EventSourceId EventSourceId;
        public EventWithPropertiesForAllSupportedTypes EventAppended;
        public Model Result;

        public override IEnumerable<Type> EventTypes => [typeof(EventWithPropertiesForAllSupportedTypes)];

        public override IEnumerable<Type> Projections => [typeof(SetPropertiesProjection)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IProjectionFor<Model>>(new SetPropertiesProjection());
        }

        public override Task Establish()
        {
            EventSourceId = Guid.NewGuid().ToString();
            EventAppended = EventWithPropertiesForAllSupportedTypes.CreateWithRandomValues();

            return Task.CompletedTask;
        }

        public override async Task Because()
        {
            var observer = GetObserverForProjection<SetPropertiesProjection>();
            await observer.WaitTillActive();
            var appendResult = await EventStore.EventLog.Append(EventSourceId, EventAppended);
            await observer.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            var filter = Builders<Model>.Filter.Eq(new StringFieldDefinition<Model, string>("_id"), EventSourceId.Value);
            var result = await globalFixture.ReadModels.Database.GetCollection<Model>().FindAsync(filter);
            Result = result.FirstOrDefault();
        }
    }

    [Fact] void should_set_the_string_value() => Fixture.Result.StringValue.ShouldEqual(Fixture.EventAppended.StringValue);
    [Fact] void should_set_the_bool_value() => Fixture.Result.BoolValue.ShouldEqual(Fixture.EventAppended.BoolValue);
    [Fact] void should_set_the_int_value() => Fixture.Result.IntValue.ShouldEqual(Fixture.EventAppended.IntValue);
    [Fact] void should_set_the_float_value() => Fixture.Result.FloatValue.ShouldEqual(Fixture.EventAppended.FloatValue);
    [Fact] void should_set_the_double_value() => Fixture.Result.DoubleValue.ShouldEqual(Fixture.EventAppended.DoubleValue);
    [Fact] void should_set_the_enum_value() => Fixture.Result.EnumValue.ShouldEqual(Fixture.EventAppended.EnumValue);
    [Fact] void should_set_the_guid_value() => Fixture.Result.GuidValue.ShouldEqual(Fixture.EventAppended.GuidValue);
    [Fact] void should_set_the_date_time_value() => Fixture.Result.DateTimeValue.ShouldEqual(Fixture.EventAppended.DateTimeValue);
    [Fact] void should_set_the_date_only_value() => Fixture.Result.DateOnlyValue.ShouldEqual(Fixture.EventAppended.DateOnlyValue);
    [Fact] void should_set_the_time_only_value() => Fixture.Result.TimeOnlyValue.ShouldEqual(Fixture.EventAppended.TimeOnlyValue);
    [Fact] void should_set_the_date_time_offset_value() => Fixture.Result.DateTimeOffsetValue.ShouldEqual(Fixture.EventAppended.DateTimeOffsetValue);
    [Fact] void should_set_the_string_concept_value() => Fixture.Result.StringConceptValue.ShouldEqual(Fixture.EventAppended.StringConceptValue);
    [Fact] void should_set_the_bool_concept_value() => Fixture.Result.BoolConceptValue.ShouldEqual(Fixture.EventAppended.BoolConceptValue);
    [Fact] void should_set_the_int_concept_value() => Fixture.Result.IntConceptValue.ShouldEqual(Fixture.EventAppended.IntConceptValue);
    [Fact] void should_set_the_float_concept_value() => Fixture.Result.FloatConceptValue.ShouldEqual(Fixture.EventAppended.FloatConceptValue);
    [Fact] void should_set_the_double_concept_value() => Fixture.Result.DoubleConceptValue.ShouldEqual(Fixture.EventAppended.DoubleConceptValue);
    [Fact] void should_set_the_guid_concept_value() => Fixture.Result.GuidConceptValue.ShouldEqual(Fixture.EventAppended.GuidConceptValue);
}
