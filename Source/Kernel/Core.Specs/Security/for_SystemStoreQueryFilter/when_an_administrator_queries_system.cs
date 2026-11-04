// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_SystemStoreQueryFilter;

public class when_an_administrator_queries_system : given.a_query_filter
{
    async Task Because() => _result = await _filter.OnPerform(ContextFor("Cratis.Chronicle.Sequences.Something", ("eventStore", "System")));

    [Fact] void should_not_refuse() => _result.ShouldBeNull();
}
