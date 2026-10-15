// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_multiple_read_models;

public class and_a_root_from_also_joins_a_child : given.an_order_with_a_root_from_and_child_join
{
    IEnumerable<ExpandoObject> _result;

    async Task Because() => _result = await _grain.Process(EventStoreNamespaceName.Default, _events);

    [Fact] void should_not_create_a_product_keyed_order() => _result.Count().ShouldEqual(1);
    [Fact] void should_join_the_existing_line() => LinesOf(ReadModelFor(_result, Order))[0]["ProductName"].ShouldEqual("Electric kettle");
}
