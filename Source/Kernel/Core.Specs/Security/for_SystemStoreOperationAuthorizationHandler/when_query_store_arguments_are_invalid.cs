// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Arc.Queries;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_query_store_arguments_are_invalid : given.a_query_operation_policy
{
    readonly List<bool> _allowed = [];

    async Task Because()
    {
        foreach (var value in new object?[] { null, "", " ", "System#namespace", "application#namespace", 7, new StringBuilder("application"), new[] { "application" } })
        {
            _allowed.Add(await Allows(nameof(given.operation_queries.Ordinary), new QueryArguments { ["eventStore"] = value! }));
        }
        _allowed.Add(await Allows(nameof(given.operation_queries.Ordinary)));
        _allowed.Add(await Allows(nameof(given.operation_queries.Ambiguous), new() { ["eventStore"] = "application" }));
    }

    [Fact] void should_refuse_every_invalid_scope_even_in_full_audit_mode() => _allowed.ShouldEachConformTo(_ => !_);
    [Fact] void should_not_audit_allow_an_unresolved_operation() => _logger.Entries.ShouldBeEmpty();
}
