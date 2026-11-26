// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_an_ordinary_store_query_is_enforced : given.a_query_operation_policy
{
    void Establish()
    {
        _enforce = true;
        Query(nameof(given.operation_queries.Ordinary), new() { ["eventStore"] = "application" });
    }

    Task Because() => AuthorizeOperation();

    [Fact] void should_allow_without_administrator_privilege() => _context.HasSucceeded.ShouldBeTrue();
    [Fact] void should_not_audit_a_denial() => _logger.Entries.ShouldBeEmpty();
}
