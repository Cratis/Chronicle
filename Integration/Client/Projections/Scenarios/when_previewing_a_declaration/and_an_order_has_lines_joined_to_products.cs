// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using System.Text.Json;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Integration.for_ReadModels;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_previewing_a_declaration.and_an_order_has_lines_joined_to_products.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_previewing_a_declaration;

/// <summary>
/// Previewing a declaration returns the documents saving the same declaration persists, for keyed child updates,
/// child removals and joins from another event source (Cratis/Chronicle#4727).
/// </summary>
/// <param name="context">The test context.</param>
[Collection(ChronicleCollection.Name)]
public class and_an_order_has_lines_joined_to_products(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public const string ProjectionIdentifier = "PreviewedOrderProjection";

        public static readonly string Declaration = $"""
            projection {ProjectionIdentifier} => {typeof(PreviewedOrder).FullName}
              every
                LastUpdated = $eventContext.occurred
              from PreviewedOrderPlaced
                CustomerName = CustomerName
                IsShipped = false
              from PreviewedOrderShipped
                IsShipped = true
              from PreviewedItemAddedToOrder
                increment ItemCount
              from PreviewedItemRemovedFromOrder
                decrement ItemCount
              from PreviewedItemQuantityAdjusted
              children Lines identified by ProductId
                from PreviewedItemAddedToOrder key ProductId
                  parent $eventContext.eventSourceId
                  Quantity = Quantity
                from PreviewedItemQuantityAdjusted key ProductId
                  parent $eventContext.eventSourceId
                  Quantity = Quantity
                remove with PreviewedItemRemovedFromOrder key ProductId
                  parent $eventContext.eventSourceId
                join Product on ProductId
                  with PreviewedProductRegistered
                    ProductName = Name
                  with PreviewedProductRenamed
                    ProductName = Name
              remove with PreviewedOrderCancelled
            """;

        static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        public Guid OrderId { get; } = Guid.NewGuid();
        public Guid Kettle { get; } = Guid.NewGuid();
        public Guid Mug { get; } = Guid.NewGuid();
        public IEnumerable<string> SaveErrors { get; private set; } = [];
        public PreviewedOrder Saved { get; private set; }
        public IReadOnlyList<PreviewedOrder> Previewed { get; private set; } = [];

        public override IEnumerable<Type> EventTypes =>
        [
            typeof(PreviewedOrderPlaced),
            typeof(PreviewedOrderShipped),
            typeof(PreviewedOrderCancelled),
            typeof(PreviewedItemAddedToOrder),
            typeof(PreviewedItemQuantityAdjusted),
            typeof(PreviewedItemRemovedFromOrder),
            typeof(PreviewedProductRegistered),
            typeof(PreviewedProductRenamed)
        ];

        async Task Because()
        {
            await EventStore.ReadModels.Register<PreviewedOrder>();

            var services = ((IChronicleServicesAccessor)EventStore.Connection).Services;
            var save = await services.Projections.Save(new()
            {
                EventStore = EventStore.Name,
                Namespace = EventStore.Namespace,
                Declaration = Declaration
            });
            SaveErrors = save.Errors.Select(_ => _.Message).ToArray();

            var handler = new ProjectionHandler(EventStore, ProjectionIdentifier, typeof(PreviewedOrder), typeof(PreviewedOrder).FullName!, EventSequenceId.Log);
            await handler.WaitTillActive();

            var start = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
            (Guid EventSourceId, object Event)[] history =
            [
                (Kettle, new PreviewedProductRegistered("Kettle")),
                (Mug, new PreviewedProductRegistered("Mug")),
                (OrderId, new PreviewedOrderPlaced("Ada")),
                (OrderId, new PreviewedItemAddedToOrder(Kettle, 1)),
                (OrderId, new PreviewedItemAddedToOrder(Mug, 2)),
                (OrderId, new PreviewedItemQuantityAdjusted(Kettle, 3)),
                (Kettle, new PreviewedProductRenamed("Electric kettle")),
                (OrderId, new PreviewedItemRemovedFromOrder(Mug)),
                (OrderId, new PreviewedOrderShipped())
            ];

            var lastSequenceNumber = EventSequenceNumber.First;
            for (var index = 0; index < history.Length; index++)
            {
                var (eventSourceId, @event) = history[index];
                var result = await EventStore.EventLog.Append(eventSourceId.ToString(), @event, occurred: start.AddSeconds(index));
                lastSequenceNumber = result.SequenceNumber;
            }

            await handler.WaitTillReachesEventSequenceNumber(lastSequenceNumber);
            Saved = await StoredReadModelDocument.ReadInstance<PreviewedOrder>(EventStore, OrderId.ToString());

            var preview = await EventStore.Projections.Query(Declaration);
            Previewed = [.. preview.ReadModelEntries.Select(entry => JsonSerializer.Deserialize<PreviewedOrder>(entry, _jsonOptions)!)];
        }
    }

    [Fact] void should_save_the_declaration() => Context.SaveErrors.ShouldBeEmpty();
    [Fact] void should_preview_only_the_order() => Context.Previewed.Count.ShouldEqual(1);
    [Fact] void should_preview_the_saved_customer_name() => Context.Previewed[0].CustomerName.ShouldEqual(Context.Saved.CustomerName);
    [Fact] void should_preview_the_saved_shipping_state() => Context.Previewed[0].IsShipped.ShouldEqual(Context.Saved.IsShipped);
    [Fact] void should_preview_the_saved_item_count() => Context.Previewed[0].ItemCount.ShouldEqual(Context.Saved.ItemCount);
    [Fact] void should_preview_the_saved_last_updated() => Context.Previewed[0].LastUpdated.ShouldEqual(Context.Saved.LastUpdated);
    [Fact] void should_preview_the_saved_lines() => Context.Previewed[0].Lines.ShouldContainOnly(Context.Saved.Lines);
    [Fact] void should_save_only_the_kettle_line() => Context.Saved.Lines.Select(_ => _.ProductId).ShouldContainOnly(Context.Kettle);
    [Fact] void should_save_the_adjusted_quantity_and_renamed_product() => Context.Saved.Lines.Single().ShouldEqual(new PreviewedOrderLine(Context.Kettle, 3, "Electric kettle"));
}

[EventType]
public record PreviewedOrderPlaced(string CustomerName);

[EventType]
public record PreviewedOrderShipped;

[EventType]
public record PreviewedOrderCancelled;

[EventType]
public record PreviewedItemAddedToOrder(Guid ProductId, int Quantity);

[EventType]
public record PreviewedItemQuantityAdjusted(Guid ProductId, int Quantity);

[EventType]
public record PreviewedItemRemovedFromOrder(Guid ProductId);

[EventType]
public record PreviewedProductRegistered(string Name);

[EventType]
public record PreviewedProductRenamed(string Name);

public record PreviewedOrderLine(Guid ProductId, int Quantity, string ProductName);

public record PreviewedOrder(Guid Id, string CustomerName, bool IsShipped, int ItemCount, DateTimeOffset LastUpdated, IEnumerable<PreviewedOrderLine> Lines);

#pragma warning restore SA1402 // File may only contain a single type
