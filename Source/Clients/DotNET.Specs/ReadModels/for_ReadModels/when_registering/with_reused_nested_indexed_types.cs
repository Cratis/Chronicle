// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_registering;

public class with_reused_nested_indexed_types : Specification
{
    record Model(Address Billing, Address Shipping, Model Parent);
    [CompositeIndex(nameof(City), nameof(PostalCode))]
    record Address([property: Index] string City, string PostalCode);

    Contracts.ReadModels.IndexDefinition[] _indexes;

    void Because() => _indexes = new ReadModelIndexes(new DefaultNamingPolicy()).For(typeof(Model)).ToArray();

    [Fact] void should_collect_billing_scalar_indexes() => _indexes.Count(index => index.PropertyPath == "Billing.City" && index.Properties.Count == 0).ShouldEqual(1);
    [Fact] void should_collect_shipping_scalar_indexes() => _indexes.Count(index => index.PropertyPath == "Shipping.City" && index.Properties.Count == 0).ShouldEqual(1);
    [Fact] void should_collect_billing_composite_indexes() => _indexes.Single(index => index.PropertyPath == "Billing.City" && index.Properties.Count == 2).Properties.Select(property => property.Path).ShouldEqual(["Billing.City", "Billing.PostalCode"]);
    [Fact] void should_collect_shipping_composite_indexes() => _indexes.Single(index => index.PropertyPath == "Shipping.City" && index.Properties.Count == 2).Properties.Select(property => property.Path).ShouldEqual(["Shipping.City", "Shipping.PostalCode"]);
    [Fact] void should_stop_at_a_recursive_type() => _indexes.Length.ShouldEqual(4);
}
