// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

public class when_inventorying_same_name_query_overloads : Specification
{
    string _first;
    string _second;
    string[] _queries;

    void Establish()
    {
        var typeName = typeof(given.overloaded_store_queries).FullName;
        _first = $"{typeName}.Query(System.String, System.Int32)";
        _second = $"{typeName}.Query(System.Int32, System.String)";
    }

    void Because() => _queries = given.store_scoped_query_inventory.For(typeof(given.overloaded_store_queries)).ToArray();

    [Fact] void should_distinguish_overloads_by_ordered_parameter_types() => _queries.ShouldContainOnly([_first, _second]);
    [Fact] void should_detect_an_overload_missing_from_the_inventory() => _queries.Where(name => !new[] { _first }.Contains(name)).ShouldContainOnly([_second]);
    [Fact] void should_detect_a_stale_overload_in_the_inventory() => new[] { _first, _second }.Where(name => !_queries.Where(query => query != _second).Contains(name)).ShouldContainOnly([_second]);
}
