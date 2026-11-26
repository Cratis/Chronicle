// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_an_administrator_runs_queries : given.a_query_operation_policy
{
    readonly List<bool> _allowed = [];

    void Establish()
    {
        _enforce = true;
        _enforceDestructive = true;
        _authorization.IsAuthorized(Arg.Any<ClaimsPrincipal>()).Returns(true);
    }

    async Task Because()
    {
        _allowed.Add(await Allows(nameof(given.operation_queries.Ordinary), new() { ["eventStore"] = "System" }));
        _allowed.Add(await Allows(nameof(given.operation_queries.Destructive), new() { ["eventStore"] = "System+namespace" }));
        _allowed.Add(await Allows(nameof(given.operation_queries.Kernel)));
    }

    [Fact] void should_allow_each_authorized_operation() => _allowed.ShouldEachConformTo(_ => _);
    [Fact] void should_not_log_a_denial() => _logger.Entries.ShouldBeEmpty();
}
