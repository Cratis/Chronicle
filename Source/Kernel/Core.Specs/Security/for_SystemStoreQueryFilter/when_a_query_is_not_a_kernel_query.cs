// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_SystemStoreQueryFilter;

public class when_a_query_is_not_a_kernel_query : given.a_query_filter
{
    void Establish() => _caller = CallerNamed(Anonymous);

    async Task Because() => _result = await _filter.OnPerform(ContextFor("Application.Queries.Something", ("eventStore", "System"), ("namespace", "a#b")));

    [Fact] void should_not_give_a_verdict() => _result.ShouldBeNull();
}
