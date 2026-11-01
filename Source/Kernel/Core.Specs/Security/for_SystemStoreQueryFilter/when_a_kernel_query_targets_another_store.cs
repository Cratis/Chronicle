// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_SystemStoreQueryFilter;

public class when_a_kernel_query_targets_another_store : given.a_query_filter
{
    void Establish() => _caller = CallerNamed(Anonymous);

    async Task Because() => _result = await _filter.OnPerform(ContextFor("Cratis.Chronicle.Sequences.Something", ("eventStore", "Store"), ("namespace", "Default")));

    [Fact] void should_not_give_a_verdict() => _result.ShouldBeNull();
    [Fact] void should_not_consult_the_administrator_roster() => _privileges.ReceivedCalls().ShouldBeEmpty();
}
