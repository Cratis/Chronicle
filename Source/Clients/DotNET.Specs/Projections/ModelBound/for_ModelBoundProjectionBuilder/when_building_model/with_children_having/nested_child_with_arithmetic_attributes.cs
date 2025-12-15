// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_having;

public class nested_child_with_arithmetic_attributes : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([
            typeof(StoreOpened),
            typeof(ProductAdded),
            typeof(StockReceived),
            typeof(StockSold),
            typeof(PriceAdjusted),
            typeof(StockCountUpdated)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(RetailChain));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();

    [Fact] void should_have_nested_children_for_products()
    {
        var storeChildren = _result.Children[nameof(RetailChain.Stores)];
        storeChildren.Children.Keys.ShouldContain(nameof(Store.Products));
    }

    [Fact] void should_have_from_definition_for_stock_received_in_nested_children()
    {
        var eventType = event_types.GetEventTypeFor(typeof(StockReceived)).ToContract();
        var storeChildren = _result.Children[nameof(RetailChain.Stores)];
        var productChildren = storeChildren.Children[nameof(Store.Products)];
        productChildren.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact] void should_have_from_definition_for_stock_sold_in_nested_children()
    {
        var eventType = event_types.GetEventTypeFor(typeof(StockSold)).ToContract();
        var storeChildren = _result.Children[nameof(RetailChain.Stores)];
        var productChildren = storeChildren.Children[nameof(Store.Products)];
        productChildren.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact] void should_have_from_definition_for_price_adjusted_in_nested_children()
    {
        var eventType = event_types.GetEventTypeFor(typeof(PriceAdjusted)).ToContract();
        var storeChildren = _result.Children[nameof(RetailChain.Stores)];
        var productChildren = storeChildren.Children[nameof(Store.Products)];
        productChildren.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact] void should_have_from_definition_for_stock_count_updated_in_nested_children()
    {
        var eventType = event_types.GetEventTypeFor(typeof(StockCountUpdated)).ToContract();
        var storeChildren = _result.Children[nameof(RetailChain.Stores)];
        var productChildren = storeChildren.Children[nameof(Store.Products)];
        productChildren.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact] void should_add_stock_quantity_from_stock_received()
    {
        var eventType = event_types.GetEventTypeFor(typeof(StockReceived)).ToContract();
        var storeChildren = _result.Children[nameof(RetailChain.Stores)];
        var productChildren = storeChildren.Children[nameof(Store.Products)];
        var fromDef = productChildren.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        var expression = fromDef.Properties[nameof(Product.StockQuantity)];
        expression.ShouldContain(WellKnownExpressions.Add);
        expression.ShouldContain(nameof(StockReceived.Quantity));
    }

    [Fact] void should_subtract_stock_quantity_from_stock_sold()
    {
        var eventType = event_types.GetEventTypeFor(typeof(StockSold)).ToContract();
        var storeChildren = _result.Children[nameof(RetailChain.Stores)];
        var productChildren = storeChildren.Children[nameof(Store.Products)];
        var fromDef = productChildren.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        var expression = fromDef.Properties[nameof(Product.StockQuantity)];
        expression.ShouldContain(WellKnownExpressions.Subtract);
        expression.ShouldContain(nameof(StockSold.Quantity));
    }

    [Fact] void should_increment_price_changes_from_price_adjusted()
    {
        var eventType = event_types.GetEventTypeFor(typeof(PriceAdjusted)).ToContract();
        var storeChildren = _result.Children[nameof(RetailChain.Stores)];
        var productChildren = storeChildren.Children[nameof(Store.Products)];
        var fromDef = productChildren.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        var expression = fromDef.Properties[nameof(Product.PriceChanges)];
        expression.ShouldEqual($"{WellKnownExpressions.Increment}()");
    }

    [Fact] void should_decrement_adjustments_remaining_from_price_adjusted()
    {
        var eventType = event_types.GetEventTypeFor(typeof(PriceAdjusted)).ToContract();
        var storeChildren = _result.Children[nameof(RetailChain.Stores)];
        var productChildren = storeChildren.Children[nameof(Store.Products)];
        var fromDef = productChildren.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        var expression = fromDef.Properties[nameof(Product.AdjustmentsRemaining)];
        expression.ShouldEqual($"{WellKnownExpressions.Decrement}()");
    }

    [Fact] void should_count_stock_updates()
    {
        var eventType = event_types.GetEventTypeFor(typeof(StockCountUpdated)).ToContract();
        var storeChildren = _result.Children[nameof(RetailChain.Stores)];
        var productChildren = storeChildren.Children[nameof(Store.Products)];
        var fromDef = productChildren.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        var expression = fromDef.Properties[nameof(Product.StockUpdateCount)];
        expression.ShouldEqual($"{WellKnownExpressions.Count}()");
    }
}

[EventType]
public record StoreOpened(StoreId Id, StoreName Name);

[EventType]
public record ProductAdded(ProductId ProductId, ProductName Name);

[EventType]
public record StockReceived(int Quantity);

[EventType]
public record StockSold(int Quantity);

[EventType]
public record PriceAdjusted(decimal NewPrice);

[EventType]
public record StockCountUpdated();

public record ChainId(Guid Value);
public record StoreId(Guid Value);
public record StoreName(string Value);
public record ProductId(Guid Value);
public record ProductName(string Value);

public record Product(
    ProductId ProductId,
    ProductName Name,

    [AddFrom<StockReceived>(nameof(StockReceived.Quantity))]
    [SubtractFrom<StockSold>(nameof(StockSold.Quantity))]
    int StockQuantity,

    [Increment<PriceAdjusted>]
    int PriceChanges,

    [Decrement<PriceAdjusted>]
    int AdjustmentsRemaining,

    [Count<StockCountUpdated>]
    int StockUpdateCount);

public record Store(
    StoreId Id,
    StoreName Name,

    [ChildrenFrom<ProductAdded>(key: nameof(ProductAdded.ProductId), identifiedBy: nameof(Product.ProductId))]
    IEnumerable<Product> Products);

[FromEvent<ChainCreated>]
public record RetailChain(
    ChainId Id,

    [ChildrenFrom<StoreOpened>(identifiedBy: nameof(Store.Id))]
    IEnumerable<Store> Stores);

[EventType]
public record ChainCreated(ChainId Id);
