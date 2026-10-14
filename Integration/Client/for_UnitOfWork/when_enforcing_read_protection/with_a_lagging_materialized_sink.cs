// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.ReadModels;

using context = Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection.with_a_lagging_materialized_sink.context;

namespace Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection;

[Collection(ChronicleCollection.Name)]
public class with_a_lagging_materialized_sink(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_read_protection_scenario(fixture)
    {
        static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        public int StoredQuantity;
        public int Count;
        public override IEnumerable<Type> ModelBoundProjections => [];

        async Task Because()
        {
            var handler = await EventStore.Projections.Register<given.StockState>(builder => builder
                .From<given.StockSet>()
                .RemovedWith<given.StockRemoved>());
            await handler.WaitTillActive();
            var first = await EventStore.EventLog.Append(Source, new given.StockSet(1));
            (await first.WaitForCompletion()).IsSuccess.ShouldBeTrue();
            await handler.WaitTillReachesEventSequenceNumber(first.SequenceNumber);

            var services = ((IChronicleServicesAccessor)EventStore.Connection).Services;
            var definitions = (await services.Projections.GetAllDefinitions(new() { EventStore = EventStore.Name })).ToArray();
            var registrant = $"read-protection-sink-lag/{Source}";
            await services.Projections.Register(new()
            {
                EventStore = EventStore.Name,
                Owner = Contracts.Projections.ProjectionOwner.Client,
                Registrant = registrant,
                Projections = definitions
            });

            // Retirement stops the observer but retains its real sink. Registering the same shape as a
            // new passive definition avoids the automatic replay caused by changing an existing definition.
            await services.Projections.Register(new()
            {
                EventStore = EventStore.Name,
                Owner = Contracts.Projections.ProjectionOwner.Client,
                Registrant = registrant,
                FullSet = true,
                Projections = []
            });
            definitions.Single().IsActive = false;
            await services.Projections.Register(new()
            {
                EventStore = EventStore.Name,
                Owner = Contracts.Projections.ProjectionOwner.Client,
                Registrant = registrant,
                Projections = definitions
            });
            await EventStore.EventLog.Append(Source, new given.StockSet(0));
            var stored = await services.ReadModels.GetInstances(new()
            {
                EventStore = EventStore.Name,
                Namespace = EventStore.Namespace,
                ReadModel = typeof(given.StockState).GetReadModelIdentifier(),
                PageSize = 10
            });
            StoredQuantity = JsonSerializer.Deserialize<given.StockState>(stored.Instances.Single(), _jsonOptions)!.Quantity;

            CreateUnit();
            using (var frame = Unit.EnforceReadProtection())
            {
                await Acquire();
                await Commit();
            }
            Count = await StoredCount();
        }
    }

    [Fact] void should_establish_a_real_stale_sink() => Context.StoredQuantity.ShouldEqual(1);
    [Fact] void should_fold_zero_from_the_kernel_log_instead_of_using_the_sink() => Context.Read.Instance!.Quantity.ShouldEqual(0);
    [Fact] void should_not_validate_the_read_only_command() => Context.Unit.GetAppendConditions().ShouldBeEmpty();
    [Fact] void should_not_write_the_read_only_command() => Context.Count.ShouldEqual(2);
}
