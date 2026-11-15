// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Changes.for_ChildrenSortValues.when_comparing;

public class values_of_different_kinds : Specification
{
    static readonly object?[] _expected = [null, 42, "text", Guid.Empty, true, DateTimeOffset.UnixEpoch];

    object?[] _ordered;

    void Because()
    {
        object?[] values = [DateTimeOffset.UnixEpoch, true, Guid.Empty, "text", 42, null];
        _ordered = [.. values.Order(Comparer<object?>.Create(ChildrenSortValues.Compare))];
    }

    [Fact] void should_order_kinds_the_way_bson_orders_types() => _ordered.ShouldEqual(_expected);
    [Fact] void should_order_false_before_true() => ChildrenSortValues.Compare(false, true).ShouldBeLessThan(0);
}
