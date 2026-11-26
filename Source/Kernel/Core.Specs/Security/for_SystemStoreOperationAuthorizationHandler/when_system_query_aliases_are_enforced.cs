// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_system_query_aliases_are_enforced : given.a_query_operation_policy
{
    readonly List<bool> _allowed = [];

    void Establish() => _enforce = true;

    async Task Because()
    {
        foreach (var store in new[] { "System", "system", "sYsTeM", "System+namespace", "SyStEm+namespace" })
        {
            _allowed.Add(await Allows(nameof(given.operation_queries.Ordinary), new() { ["eventStore"] = store }));
        }
    }

    [Fact] void should_refuse_every_system_alias() => _allowed.ShouldEachConformTo(_ => !_);
}
