// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.Base;
using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Events;
using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.ProjectionTypes;
using MongoDB.Driver;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.when_projecting_properties;

[Collection(GlobalCollection.Name)]
public class adding_from_properties(adding_from_properties.context context) : OrleansTest<adding_from_properties.context>(context)
{
    public class context(GlobalFixture globalFixture) : IntegrationTestSetup(globalFixture)
    {
        public EventSourceId EventSourceId;
        public Model Result;
        public EventWithPropertiesForAllSupportedTypes FirstEventAppended;
        public EventWithPropertiesForAllSupportedTypes SecondEventAppended;

        public override IEnumerable<Type> EventTypes => [typeof(EventWithPropertiesForAllSupportedTypes)];

        public override IEnumerable<Type> Projections => [typeof(AddingFromPropertiesProjection)];


        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IProjectionFor<Model>>(new AddingFromPropertiesProjection());
        }

        public override Task Establish()
        {
            EventSourceId = Guid.NewGuid().ToString();
            FirstEventAppended = EventWithPropertiesForAllSupportedTypes.CreateWithRandomValues();
            SecondEventAppended = EventWithPropertiesForAllSupportedTypes.CreateWithRandomValues();

            return Task.CompletedTask;
        }

        public override async Task Because()
        {
            var observer = GetObserverForProjection<AddingFromPropertiesProjection>();
            await observer.WaitTillActive();
            await EventStore.EventLog.Append(EventSourceId, FirstEventAppended);
            var appendResult = await EventStore.EventLog.Append(EventSourceId, SecondEventAppended);
            await observer.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            var filter = Builders<Model>.Filter.Eq(new StringFieldDefinition<Model, string>("_id"), EventSourceId.Value);
            var result = await globalFixture.ReadModels.Database.GetCollection<Model>().FindAsync(filter);
            Result = result.FirstOrDefault();
        }
    }

    [Fact] void should_result_in_correct_integer_value() => Context.Result.IntValue.ShouldEqual(Context.FirstEventAppended.IntValue + Context.SecondEventAppended.IntValue);
    [Fact] void should_result_in_correct_float_value() => Math.Round(Context.Result.FloatValue, 3).ShouldEqual(Math.Round(Context.FirstEventAppended.FloatValue + Context.SecondEventAppended.FloatValue, 3));
    [Fact] void should_result_in_correct_double_value() => Context.Result.DoubleValue.ShouldEqual(Context.FirstEventAppended.DoubleValue + Context.SecondEventAppended.DoubleValue);
}
