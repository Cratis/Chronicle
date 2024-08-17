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
public class with_simple_set_operations(with_simple_set_operations.context context) : OrleansTest<with_simple_set_operations.context>(context)
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

    [Fact] void should_set_the_string_value() => Context.Result.StringValue.ShouldEqual(Context.EventAppended.StringValue);
    [Fact] void should_set_the_bool_value() => Context.Result.BoolValue.ShouldEqual(Context.EventAppended.BoolValue);
    [Fact] void should_set_the_int_value() => Context.Result.IntValue.ShouldEqual(Context.EventAppended.IntValue);
    [Fact] void should_set_the_float_value() => Context.Result.FloatValue.ShouldEqual(Context.EventAppended.FloatValue);
    [Fact] void should_set_the_double_value() => Context.Result.DoubleValue.ShouldEqual(Context.EventAppended.DoubleValue);
    [Fact] void should_set_the_enum_value() => Context.Result.EnumValue.ShouldEqual(Context.EventAppended.EnumValue);
    [Fact] void should_set_the_guid_value() => Context.Result.GuidValue.ShouldEqual(Context.EventAppended.GuidValue);
    [Fact] void should_set_the_date_time_value() => Context.Result.DateTimeValue.ShouldEqual(Context.EventAppended.DateTimeValue);
    [Fact] void should_set_the_date_only_value() => Context.Result.DateOnlyValue.ShouldEqual(Context.EventAppended.DateOnlyValue);
    [Fact] void should_set_the_time_only_value() => Context.Result.TimeOnlyValue.ShouldEqual(Context.EventAppended.TimeOnlyValue);
    [Fact] void should_set_the_date_time_offset_value() => Context.Result.DateTimeOffsetValue.ShouldEqual(Context.EventAppended.DateTimeOffsetValue);
    [Fact] void should_set_the_string_concept_value() => Context.Result.StringConceptValue.ShouldEqual(Context.EventAppended.StringConceptValue);
    [Fact] void should_set_the_bool_concept_value() => Context.Result.BoolConceptValue.ShouldEqual(Context.EventAppended.BoolConceptValue);
    [Fact] void should_set_the_int_concept_value() => Context.Result.IntConceptValue.ShouldEqual(Context.EventAppended.IntConceptValue);
    [Fact] void should_set_the_float_concept_value() => Context.Result.FloatConceptValue.ShouldEqual(Context.EventAppended.FloatConceptValue);
    [Fact] void should_set_the_double_concept_value() => Context.Result.DoubleConceptValue.ShouldEqual(Context.EventAppended.DoubleConceptValue);
    [Fact] void should_set_the_guid_concept_value() => Context.Result.GuidConceptValue.ShouldEqual(Context.EventAppended.GuidConceptValue);
}
