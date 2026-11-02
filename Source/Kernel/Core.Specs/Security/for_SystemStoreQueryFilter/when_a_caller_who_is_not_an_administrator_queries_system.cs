// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_SystemStoreQueryFilter;

public class when_a_caller_who_is_not_an_administrator_queries_system : given.a_query_filter
{
    [Theory]
    [MemberData(nameof(NonAdministrators))]
    public async Task should_refuse_the_query(string caller)
    {
        _caller = CallerNamed(caller);
        var result = await _filter.OnPerform(ContextFor("Cratis.Chronicle.Sequences.Something", ("EventStore", "system")));
        result.IsAuthorized.ShouldBeFalse();
    }
}
