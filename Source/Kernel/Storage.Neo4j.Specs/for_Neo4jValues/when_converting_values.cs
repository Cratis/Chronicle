// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using Neo4j.Driver;

namespace Cratis.Chronicle.Storage.Neo4j.for_Neo4jValues;

public class when_converting_values : Specification
{
    static readonly Guid _guid = Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e");

    object _decimal;
    object _guidText;
    object _integer;
    object _list;
    object _nothing;
    Exception _unsupported;
    Exception _reserved;
    object _zoned;
    object _fromList;
    Dictionary<string, object?> _model;

    void Because()
    {
        _decimal = Neo4jValues.ToDriver(2.5m);
        _guidText = Neo4jValues.ToDriver(_guid);
        _integer = Neo4jValues.ToDriver(7);
        _list = Neo4jValues.ToDriver(new[] { 1, 2 });
        _nothing = Neo4jValues.ToDriver((object)null);
        _unsupported = Catch.Exception(() => Neo4jValues.ToDriver(new object()));
        _reserved = Catch.Exception(() => Neo4jValues.ToDriver(new Dictionary<string, object?> { ["_owner"] = "x" }));
        _zoned = Neo4jValues.FromDriver(new ZonedDateTime(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.FromHours(1))));
        _fromList = Neo4jValues.FromDriver(new List<object> { 1L, "a" });
        _model = Neo4jValues.FromDriver(new Dictionary<string, object>
        {
            ["name"] = "Ada",
            [GraphBookkeeping.Watermark] = 1L,
            [GraphBookkeeping.Owner] = "owner"
        });
    }

    [Fact] void should_send_a_decimal_as_a_double() => _decimal.ShouldEqual(2.5d);
    [Fact] void should_send_a_guid_as_its_text() => _guidText.ShouldEqual("0f8fad5b-d9cb-469f-a165-70867728950e");
    [Fact] void should_send_a_whole_number_as_a_long() => _integer.ShouldEqual(7L);
    [Fact] void should_send_a_list_of_converted_values() => ((IEnumerable<object>)_list).ShouldContainOnly(1L, 2L);
    [Fact] void should_send_null_as_null() => _nothing.ShouldBeNull();
    [Fact] void should_refuse_a_value_a_graph_cannot_store() => _unsupported.ShouldBeOfExactType<UnsupportedGraphValue>();
    [Fact] void should_refuse_a_property_named_like_the_bookkeeping() => _reserved.ShouldBeOfExactType<ReservedGraphPropertyName>();
    [Fact] void should_return_a_zoned_date_time_as_a_date_time_offset() => _zoned.ShouldEqual(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.FromHours(1)));
    [Fact] void should_return_a_list_with_converted_values() => ((IEnumerable<object>)_fromList).ShouldContainOnly(1L, "a");
    [Fact] void should_leave_the_bookkeeping_out_of_the_model() => _model.Keys.ShouldContainOnly("name");
}
